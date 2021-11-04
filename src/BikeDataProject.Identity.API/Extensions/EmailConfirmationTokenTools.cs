using System.Text;
using System.Threading.Tasks;
using BikeDataProject.Identity.Db;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace BikeDataProject.Identity.API.Extensions
{
    internal static class EmailConfirmationTokenTools
    {
        /// <summary>
        /// Generates an email confirmation token and encodes it in base64.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="user">The user.</param>
        /// <returns></returns>
        public static async Task<string> GenerateEmailConfirmationTokenBase64Async(this UserManager<ApplicationUser> userManager, ApplicationUser user)
        {
            var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var tokenGeneratedBytes = Encoding.UTF8.GetBytes(confirmationToken);
            return WebEncoders.Base64UrlEncode(tokenGeneratedBytes);
        }

        /// <summary>
        /// Encodes the email as base64.
        /// </summary>
        /// <param name="email">The email.</param>
        /// <returns>The encoded email.</returns>
        public static string EncodeBase64(this string email)
        {
            var tokenGeneratedBytes = Encoding.UTF8.GetBytes(email);
            return WebEncoders.Base64UrlEncode(tokenGeneratedBytes);
        }

        /// <summary>
        /// Decodes the confirmation token before validating it.
        /// </summary>
        /// <param name="confirmationTokenBase64">The encoded token.</param>
        /// <returns>The decoded token.</returns>
        public static string DecodeConfirmationToken(string confirmationTokenBase64)
        {
            var codeDecodedBytes = WebEncoders.Base64UrlDecode(confirmationTokenBase64);
            return Encoding.UTF8.GetString(codeDecodedBytes);
        }

        /// <summary>
        /// Decodes the email.
        /// </summary>
        /// <param name="encodedEmail">The encoded email.</param>
        /// <returns>The email.</returns>
        public static string DecodeBase64(this string encodedEmail)
        {
            var codeDecodedBytes = WebEncoders.Base64UrlDecode(encodedEmail);
            return Encoding.UTF8.GetString(codeDecodedBytes);
        }
    }
}