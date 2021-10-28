namespace BikeDataProject.Identity.API.Data
{
    /// <summary>
    /// Represents a user that's part of an organization.
    /// </summary>
    public class ApplicationOrganizationUser
    {
        /// <summary>
        /// The id (primary key).
        /// </summary>
        public int Id { get; set; }
        
        public int ApplicationOrganizationId { get; set; }
        public ApplicationOrganization ApplicationOrganization { get; set; }

        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; } 
        
        public int ApplicationOrganizationRoleId { get; set; }
        public ApplicationOrganizationRole ApplicationOrganizationRole { get; set; }
    }
}