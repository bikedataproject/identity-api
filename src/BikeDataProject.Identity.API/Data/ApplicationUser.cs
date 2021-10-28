using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace BikeDataProject.Identity.API.Data
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

        /// <summary>
        /// A collection of users and their organization.
        /// </summary>
        public List<ApplicationOrganizationUser> OrganizationUsers { get; set; }
    }
}