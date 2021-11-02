namespace BikeDataProject.Identity.API.Services.Mailjet
{
    /// <summary>
    /// The mailjet configuration
    /// </summary>
    public class MailjetConfiguration
    {
        /// <summary>
        /// The api key.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The api secret.
        /// </summary>
        public string ApiSecret { get; set; }
        
        /// <summary>
        /// The fitbit template id.
        /// </summary>
        public int FitbitTemplateId { get; set; }
    }
}