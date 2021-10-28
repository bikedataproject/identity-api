using Microsoft.AspNetCore.Authorization;

namespace BikeDataProject.Identity.API.Policies
{
    public class IsAdminRequirement : IAuthorizationRequirement{}
}