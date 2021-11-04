using System;
using System.Linq;
using System.Threading.Tasks;
using BikeDataProject.Identity.API.Extensions;
using BikeDataProject.Identity.API.Controllers.Integrations.Fitbit.Models;
using BikeDataProject.Identity.API.Services;
using BikeDataProject.Identity.Db;
using BikeDataProject.Identity.Db.Integrations.Fitbit;
using Fitbit.Api.Portable;
using Fitbit.Api.Portable.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BikeDataProject.Identity.API.Controllers.Integrations.Fitbit
{
    [Authorize]
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
        [AllowAnonymous]
        [HttpPost("/fitbit/authorize")]
        public async Task<ActionResult<FitbitAccountAuthorizeResponseModel>> Authorize(
            [FromBody] FitbitAccountAuthorizeModel registerModel)
        {
            if (!ModelState.IsValid) return BadRequest();
            
            // we respond with an authorize url for fitbit and nothing more
            // potentially, on callback the logged in user will be linked with their fitbit account
            //   or a new account will be created at that point.
            
            _logger.LogDebug("Authorization requested with user logged in");
            return new FitbitAccountAuthorizeResponseModel
            {
                Url = this.GenerateAuthorizeUrl(_configuration.FitbitAppCredentials, registerModel.RedirectUrl)
            };
        }

        /// <summary>
        /// Register a new user using the given email address and sends a confirmation link for fitbit account linking.
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("/fitbit/register")]
        public async Task<ActionResult> Register([FromBody] FitbitAccountRegisterModel registerModel)
        {
            if (!ModelState.IsValid) return BadRequest();
            
            // create a new user with the given email address and no password.
            // or get the existing user if there is a user with an unconfirmed password.
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
                    user = await _userManager.FindByEmailAsync(registerModel.Email);

                    if (user.EmailConfirmed)
                    {
                        // there is already a user with the given email address.
                        // TODO: send a link anyway but tell the user to login and or recover their password.
                        return Conflict();
                    }
                }
                else
                {
                    throw new Exception("Failed to create user");
                }
            }
            
            // generate a confirmation token and build the url.
            var confirmationTokenEncoded = await _userManager.GenerateEmailConfirmationTokenBase64Async(user);
            var emailEncoded = user.Email.EncodeBase64();
            var uriBuilder = new UriBuilder(registerModel.ConfirmEmailUrl)
            {
                Query = $"token={confirmationTokenEncoded}&email={emailEncoded}"
            };
            await _emailSender.SendFitbitConfirmAsync(registerModel.Email,
                uriBuilder.Uri.ToString());
            _logger.LogDebug("Sent email verification email: {Url}", uriBuilder.Uri.ToString());

            return Ok();
        }

        /// <summary>
        /// Register the fitbit user using the given code from the authorize callback to access the fitbit api.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="redirectUrl">The redirect url that was used to generate the code.</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        [AllowAnonymous]
        [HttpGet("/fitbit/register/callback")]
        public async Task<IActionResult> Register(string code, string redirectUrl)
        {
            // there are 3 possible flows here:
            // 1: a fitbit user with the same details already exists
            // - we update the details with these more recent data.
            // 2: a fitbit user doesn't exists but a user is logged in.
            // - we add the fitbit user to the logged in user account.
            // #: a fitbit user doesn't exist and a user it not logged in.
            // - we add the fitbit user linked to an anonymous account.
            
            _logger.LogDebug("Request to register: {Code}", code);

            var authenticator = new OAuth2Helper(_configuration.FitbitAppCredentials, redirectUrl);

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

                    // sign in user.
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogDebug("User created and signed in");
                }

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
                _logger.LogDebug("Fitbit user created");
            }

            return Ok();
        }
    }
}