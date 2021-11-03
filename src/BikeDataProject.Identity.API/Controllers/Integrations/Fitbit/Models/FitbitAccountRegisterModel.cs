using System.ComponentModel.DataAnnotations;

namespace BikeDataProject.Identity.API.Controllers.Integrations.Fitbit.Models
{
    /// <summary>
    /// Data model for a register request for a new user starting with a fitbit account.
    /// </summary>
    public class FitbitAccountRegisterModel
    {
        /// <summary>
        /// The optional email address.
        /// </summary>
        [EmailAddress]
        [Display(Name = "Email")]
        [Required]
        public string Email { get; set; }
        
        /// <summary>
        /// The url to use for email confirmation if an email address was given.
        /// </summary>
        [Url]
        [Required]
        public string ConfirmEmailUrl { get; set; }
    }
}