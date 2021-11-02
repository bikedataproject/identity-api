using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BikeDataProject.Identity.API.Data;
using BikeDataProject.Identity.API.Extensions;
using BikeDataProject.Identity.API.Models.AccountViewModels;
using BikeDataProject.Identity.API.Services;

namespace BikeDataProject.Identity.API.Controllers
{
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _dbContext = dbContext;
        }

        [TempData] public string ErrorMessage { get; set; }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid) return BadRequest();

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe,
                lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in");
                return Ok();
            }

            if (result.RequiresTwoFactor)
            {
                throw new NotImplementedException();
            }

            if (result.IsLockedOut)
            {
                throw new NotImplementedException();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Unauthorized();
            }
        }

        /// <summary>
        /// Confirms a users' email.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet("confirmemail")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            if (!ModelState.IsValid) return BadRequest();

            // get the user.
            var decodedEmail = email.DecodeBase64();
            var user = await _userManager.FindByEmailAsync(decodedEmail);
            if (user == null)
            {
                return BadRequest();
            }

            // decode the token and confirm email.
            var decodeToken = EmailConfirmationTokenTools.DecodeConfirmationToken(token);
            var result = await _userManager.ConfirmEmailAsync(user, decodeToken);

            if (!result.Succeeded)
            {
                // validation failed.
                return BadRequest();
            }

            // success, sign in the user.
            await _signInManager.SignInAsync(user, false);
            return Ok();
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid) return BadRequest();

            // check for an existing user.
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                // a user exists.
                // if a user exists and it has a password set then we cannot register this user.
                if (!string.IsNullOrWhiteSpace(existingUser.PasswordHash))
                {
                    return Conflict();
                }

                await _signInManager.SignInAsync(existingUser, false);

                return Ok();
            }

            // we need to create a new user.
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FunctionalName = model.FunctionalName
            };

            IdentityResult result;
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                // create a user without password, this is for logins with an external provider.
                result = await _userManager.CreateAsync(user);
            }
            else
            {
                // a password was set, this is a user registering directly.
                result = await _userManager.CreateAsync(user, model.Password);
            }

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password");

                // generate a confirmation token and build the url.
                var confirmationTokenEncoded = await _userManager.GenerateEmailConfirmationTokenBase64Async(user);
                var emailEncoded = user.Email.EncodeBase64();
                var uriBuilder = new UriBuilder(model.ConfirmEmailUrl)
                {
                    Query = $"token={confirmationTokenEncoded}&email={emailEncoded}"
                };
                await _emailSender.SendConfirmAsync(model.Email,
                    uriBuilder.Uri.ToString());
                _logger.LogDebug("Sent email verification email: {Url}", uriBuilder.Uri.ToString());

                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("User created a new account with password");
                return Ok();
            }

            AddErrors(result);

            return Unauthorized();
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out");
            return Ok();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return Redirect("/");
            }
        }

        #endregion
    }
}