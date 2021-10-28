using System.ComponentModel.DataAnnotations;

namespace BikeDataProject.Identity.API.Models.AccountViewModels
{
    /// <summary>
    /// The data to confirm an email.
    /// </summary>
    public class ConfirmEmailModel
    {
        /// <summary>
        /// The email confirmation token.
        /// </summary>
        [Required]
        public string Token { get; set; }

        /// <summary>
        /// The email to confirm.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}