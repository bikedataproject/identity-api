namespace BikeDataProject.Identity.API.Configuration
{
    /// <summary>
    /// The messaging queue config.
    /// </summary>
    public class MessagingQueueConfig
    {
        /// <summary>
        /// The host.
        /// </summary>
        public string Host { get; set; }
        
        /// <summary>
        /// The virtual host if any.
        /// </summary>
        public string VirtualHost { get; set; }
        
        /// <summary>
        /// The username.
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// The password.
        /// </summary>
        public string Password { get; set; }
        
        /// <summary>
        /// The consumer queues prefixes.
        /// </summary>
        public string ConsumerQueuesPrefix { get; set; }
    }
}