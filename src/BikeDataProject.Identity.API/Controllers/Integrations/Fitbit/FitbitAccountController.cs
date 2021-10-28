using System;
using System.Linq;
using System.Threading.Tasks;
using BikeDataProject.Identity.API.Extensions;
using BikeDataProject.Identity.API.Controllers.Integrations.Fitbit.Models;
using BikeDataProject.Identity.API.Data;
using BikeDataProject.Identity.API.Data.Integrations.Fitbit;
using BikeDataProject.Identity.API.Services;
using Fitbit.Api.Portable;
using Fitbit.Api.Portable.OAuth2;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BikeDataProject.Identity.API.Controllers.Integrations.Fitbit
{
    public class FitbitAccountController : ControllerBase
    {
        private readonly ILogger<FitbitAccountController> _logger;
        private readonly FitbitAccountControllerSettings _configuration;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        
        public FitbitAccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, 
            IEmailSender emailSender, ApplicationDbContext db, FitbitAccountControllerSettings configuration, ILogger<FitbitAccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }
        
        /// <summary>
        /// Gets the authorize url to redirect to to start authorization.
        /// </summary>
        /// <returns></returns>
        [HttpPost("/fitbit/register")]
        public async Task<ActionResult<FitbitAccountRegisterResponseModel>> Register([FromBody] FitbitAccountRegisterModel registerModel)
        {
            // there are 3 possible flows here:
            // 1: a user is logged in
            //    - if an email address is provided this is seen as a conflict, the user is already logged in.
            //    - we respond with an authorize url for fitbit and nothing more, on callback the logged in user will be linked with their fitbit account.
            // 2: email address is not provided.
            //    - we respond with an authorize url for fitbit and nothing more.
            // 3: email address is provided.
            //   we cannot accept an email address without confirming it so we send a url to the users' email that:
            //    - confirms their email address.
            //    - starts the authorization flow.

            // flow #1
            if (_signInManager.IsSignedIn(this.User))
            {
                if (!string.IsNullOrWhiteSpace(registerModel.Email))
                {
                    // when the user is logged in, it doesn't make sense to give their email again
                    // so we expect this to happen without an email provided.
                    return Conflict();
                }
                
                _logger.LogDebug("Authorization requested with user logged in");
                return new FitbitAccountRegisterResponseModel {
                    Url = this.GenerateAuthorizeUrl(_configuration.FitbitAppCredentials, registerModel.RedirectUrl)
                };
            }
            
            // flow #2
            if (string.IsNullOrWhiteSpace(registerModel.Email)) 
            {
                _logger.LogDebug("Authorization requested without email");
                return new FitbitAccountRegisterResponseModel {
                    Url = this.GenerateAuthorizeUrl(_configuration.FitbitAppCredentials, registerModel.RedirectUrl)
                };
            }

            // flow #3
            // create a new user with the given email address and no password.
            if (string.IsNullOrWhiteSpace(registerModel.ConfirmEmailUrl))
            {
                return BadRequest();
            }
            var user = new ApplicationUser()
            {
                UserName = registerModel.Email,
                Email = registerModel.Email
            };
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(x => x.Code == "DuplicateEmail"))
                {
                    return Conflict();
                }
                throw new Exception("Failed to create user");
            }
            
            // generate a confirmation token and build the url.
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenBase64Async(user);
            var uriBuilder = new UriBuilder(registerModel.ConfirmEmailUrl)
            {
                Query = $"token={confirmationToken}&email={user.Email}"
            };
            await _emailSender.SendEmailAsync(registerModel.Email, "Bike Data Project email confirmation",
                uriBuilder.Uri.ToString());
            _logger.LogDebug("Sent email verification email: {Url}", uriBuilder.Uri.ToString());

            return new FitbitAccountRegisterResponseModel()
            {
                EmailSent = true
            };
        }

        /// <summary>
        /// Register the fitbit user using the given code from the authorize callback to access the fitbit api.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        [HttpGet("/fitbit/register/callback")]
        public async Task<IActionResult> Register(string code)
        {
            // there are 3 possible flows here:
            // 1: a fitbit user with the same details already exists
            // - we update the details with these more recent data.
            // 2: a fitbit user doesn't exists but a user is logged in.
            // - we add the fitbit user to the logged in user account.
            // #: a fitbit user doesn't exist and a user it not logged in.
            // - we add the fitbit user linked to an anonymous account.
            
            _logger.LogDebug("Request to register: {Code}", code);

            var callback = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/register";
            var authenticator = new OAuth2Helper(_configuration.FitbitAppCredentials, callback);

            _logger.LogDebug("Authenticator created");

            var newToken = await authenticator.ExchangeAuthCodeForAccessTokenAsync(code);

            _logger.LogDebug("Token exchanged");

            if (newToken == null)
            {
                _logger.LogError("Getting access token failed!");
                return new NotFoundResult();
            }

            var fitbitUser = (from accessTokens in _db.FitbitUsers
                where accessTokens.UserId == newToken.UserId
                select accessTokens).FirstOrDefault();
            if (fitbitUser != null)
            {
                // update fitbituser.
                fitbitUser.Scope = newToken.Scope;
                fitbitUser.Token = newToken.Token;
                fitbitUser.ExpiresIn = newToken.ExpiresIn;
                fitbitUser.RefreshToken = newToken.RefreshToken;
                fitbitUser.TokenType = newToken.TokenType;
                fitbitUser.TokenCreated = DateTime.UtcNow;

                _db.FitbitUsers.Update(fitbitUser);
                await _db.SaveChangesAsync();
            }
            else
            {
                // a new user, we need to create or get the current application user.
                ApplicationUser user;
                if (_signInManager.IsSignedIn(this.User))
                {
                    // get the existing user, there is a user logged in,
                    // we assume it's this user wanting to add the current fitbit account.
                    user = await _userManager.GetUserAsync(User);

                    if (user == null)
                    {
                        throw new ApplicationException(
                            $"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
                    }
                }
                else
                {
                    // get details from the fitbit api to initialize the local user account.
                    var accessToken = new OAuth2AccessToken()
                    {
                        Scope = newToken.Scope,
                        ExpiresIn = newToken.ExpiresIn,
                        RefreshToken = newToken.RefreshToken,
                        Token = newToken.Token,
                        TokenType = newToken.TokenType,
                        UserId = newToken.UserId
                    };
                    var fitbitClient = new FitbitClient(_configuration.FitbitAppCredentials, accessToken);
                    var fitbitUserProfile = await fitbitClient.GetUserProfileAsync();

                    // create user.
                    user = new ApplicationUser()
                    {
                        UserName = fitbitUserProfile.DisplayName
                    };
                    await _db.Users.AddAsync(user);
                    await _db.SaveChangesAsync();

                    // create fitbit user.
                    fitbitUser = new FitbitUser
                    {
                        ApplicationUserId = user.Id,
                        ApplicationUser = user,
                        UserId = newToken.UserId,
                        Scope = newToken.Scope,
                        Token = newToken.Token,
                        ExpiresIn = newToken.ExpiresIn,
                        RefreshToken = newToken.RefreshToken,
                        TokenType = newToken.TokenType,
                        TokenCreated = DateTime.UtcNow
                    };
                    await _db.FitbitUsers.AddAsync(fitbitUser);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("User created a new account using a fitbit account");

                    // sign in user.
                    await _signInManager.SignInAsync(user, isPersistent: false);
                }
            }

            return Ok();
        }
    }
}