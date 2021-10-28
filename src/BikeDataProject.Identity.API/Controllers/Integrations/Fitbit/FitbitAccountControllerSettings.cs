using Fitbit.Api.Portable;

namespace BikeDataProject.Identity.API.Controllers.Integrations.Fitbit
{
    /// <summary>
    /// Settings for the fitbit register controller.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class FitbitAccountControllerSettings
    {
        /// <summary>
        /// The fitbit app credentials.
        /// </summary>
        public FitbitAppCredentials FitbitAppCredentials { get; set; }
    }
}