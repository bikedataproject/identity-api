using System.ComponentModel.DataAnnotations;

namespace BikeDataProject.Identity.API.Controllers.Integrations.Fitbit.Models
{
    /// <summary>
    /// Data model for an authorize request to fitbit.
    /// </summary>
    public class FitbitAccountAuthorizeModel
    {
        /// <summary>
        /// The redirect url to let fitbit redirect to.
        /// </summary>
        [Url]
        [Required]
        public string RedirectUrl { get; set; }
    }
}