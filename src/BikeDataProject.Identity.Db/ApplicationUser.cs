using Microsoft.AspNetCore.Identity;

namespace BikeDataProject.Identity.Db
{
    /// <summary>
    /// Represents a basic user.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// The admin flag.
        /// </summary>
        public bool IsAdmin { get; set; }
        
        /// <summary>
        /// The functional name.
        /// </summary>
        public string FunctionalName { get; set; }
    }
}