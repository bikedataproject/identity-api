namespace BikeDataProject.Identity.API.Data
{
    /// <summary>
    /// Represents a role a user can have in an organization.
    /// </summary>
    public class ApplicationOrganizationRole
    {
        /// <summary>
        /// The id (primary key).
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// The key of the role, a string identifying it.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The visible name of the role.
        /// </summary>
        public string Name { get; set; }
    }
}