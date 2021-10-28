using Fitbit.Api.Portable;
using Fitbit.Api.Portable.OAuth2;

namespace BikeDataProject.Identity.API.Controllers.Integrations.Fitbit
{
    internal static class FitbitAccountControllerHelper
    {
        /// <summary>
        /// Generates an authorize URL to authorize with the fitbit api.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="fitbitAppCredentials"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static string GenerateAuthorizeUrl(this FitbitAccountController controller, 
            FitbitAppCredentials fitbitAppCredentials, string callback)
        {
            var authenticator = new OAuth2Helper(fitbitAppCredentials, callback);

            var scopes = new[] {"activity","profile","location"};
            return authenticator.GenerateAuthUrl(scopes, null);
        }
    }
}