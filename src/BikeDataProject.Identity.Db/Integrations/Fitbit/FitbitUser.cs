using System;

namespace BikeDataProject.Identity.Db.Integrations.Fitbit
{
    /// <summary>
    /// Represents a fitbit user.
    /// </summary>
    public class FitbitUser
    {
        /// <summary>
        /// The id (primary key).
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// The parent application user id.
        /// </summary>
        public string ApplicationUserId { get; set; }

        /// <summary>
        /// The parent application user.
        /// </summary>
        public ApplicationUser ApplicationUser { get; set; }
        
        /// <summary>
        /// The access token.
        /// </summary>
        public string Token { get; set; }
        
        /// <summary>
        /// The type of the access token.
        /// </summary>
        public string TokenType { get; set; }
        
        /// <summary>
        /// The scope of the access token.
        /// </summary>
        public string Scope { get; set; }
        
        /// <summary>
        /// The lifetime of the token in seconds.
        /// </summary>
        public int ExpiresIn { get; set; }
        
        /// <summary>
        /// The refresh token, for use when the token is expired.
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// The fitbit user id.
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// The timestamp for when the token was created.
        /// </summary>
        public DateTime TokenCreated { get; set; }
    }
}