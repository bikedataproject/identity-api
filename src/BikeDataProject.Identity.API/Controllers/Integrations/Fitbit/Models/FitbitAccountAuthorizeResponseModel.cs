namespace BikeDataProject.Identity.API.Controllers.Integrations.Fitbit.Models
{
    /// <summary>
    /// Response after an authorize request.
    /// </summary>
    public class FitbitAccountAuthorizeResponseModel
    {
        /// <summary>
        /// The authorize url to authorize with the fitbit API.
        /// </summary>
        public string Url { get; set; }
    }
}