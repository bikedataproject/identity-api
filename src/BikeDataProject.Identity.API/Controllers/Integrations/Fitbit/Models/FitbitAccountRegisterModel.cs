using System.ComponentModel.DataAnnotations;

namespace BikeDataProject.Identity.API.Controllers.Integrations.Fitbit.Models
{
    /// <summary>
    /// Data model for a register request for a fitbit user.
    /// </summary>
    public class FitbitAccountRegisterModel
    {
        /// <summary>
        /// The optional email address.
        /// </summary>
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        /// <summary>
        /// The url to use for email confirmation if an email address was given.
        /// </summary>
        public string ConfirmEmailUrl { get; set; }
        
        /// <summary>
        /// The redirect url to let fitbit redirect to.
        /// </summary>
        public string RedirectUrl { get; set; }
    }
}