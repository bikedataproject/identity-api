namespace BikeDataProject.Identity.API.Controllers.Integrations.Fitbit.Models
{
    /// <summary>
    /// The response data after registering a new fitbit user.
    /// </summary>
    public class FitbitAccountRegisterResponseModel
    {
        /// <summary>
        /// The authorize url to authorize with the fitbit API.
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// An email was sent.
        /// </summary>
        public bool EmailSent { get; set; }
    }
}