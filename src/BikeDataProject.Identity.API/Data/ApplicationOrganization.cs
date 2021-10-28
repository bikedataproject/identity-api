using System.Collections.Generic;

namespace BikeDataProject.Identity.API.Data
{
    /// <summary>
    /// Represents an organization, a client, a collection of users.
    /// </summary>
    public class ApplicationOrganization
    {
        /// <summary>
        /// The id (primary key).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The key name of the organization, this is used in URLs to identify the organization.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The visible name of the organization.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A collection of users and their roles.
        /// </summary>
        public List<ApplicationOrganizationUser> Users { get; set; }
    }
}