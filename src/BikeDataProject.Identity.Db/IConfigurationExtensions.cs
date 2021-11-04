using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BikeDataProject.Identity.Db
{
    /// <summary>
    /// Contains configuration extensions.
    /// </summary>
    internal static class IConfigurationExtensions
    {
        /// <summary>
        /// Loads deploy time appsettings using a path defined in the build-time settings.
        /// </summary>ı
        /// <param name="configurationBuilder">The configuration builder.</param>
        /// <returns>The deploy time settings.</returns>
        public static (string deployTimeSettings, string envVarPrefix) GetDeployTimeSettings(this IConfigurationBuilder configurationBuilder)
        {
            // get deploy time settings if present.
            var configuration = configurationBuilder.Build();
            var deployTimeSettings = configuration["deploy-time-settings"] ?? "/var/app/config/appsettings.json";
            configurationBuilder = configurationBuilder.AddJsonFile(deployTimeSettings, true, true);

            // get environment variable prefix.
            // do this after the deploy time settings to make sure this is configurable at deploytime.
            configuration = configurationBuilder.Build();
            var envVarPrefix = configuration["env-var-prefix"] ?? "ANYWAYS_";

            return (deployTimeSettings, envVarPrefix);
        }
        
        /// <summary>
        /// Gets a typed value and if not found returns a default value.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="configuration">The configuration.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default.</param>
        /// <returns>The configured or default value.</returns>
        public static T GetValueOrDefault<T>(this IConfiguration configuration, string key, T defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(configuration[key])) return defaultValue;

            return configuration.GetValue<T>(key);
        }
        
        /// <summary>
        /// Gets a string value and if not found returns a default value.
        /// </summary>
        /// <remarks>
        /// - When there is a key defined with suffix _FILE the file contents are read and returned.
        ///   When the file doesn't exist, there is an exception, the default will not be used.
        /// </remarks>
        /// <param name="configuration">The configuration.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default.</param>
        /// <returns>The configured or default value.</returns>
        public static string GetValueOrDefault(this IConfiguration configuration, string key, string defaultValue = default)
        {
            var keyForFile = $"{key}_FILE";
            var file = configuration[keyForFile];
            if (string.IsNullOrWhiteSpace(file))
                return string.IsNullOrWhiteSpace(configuration[key])
                    ? defaultValue
                    : configuration.GetValue<string>(key);
            // when there is a key defined with suffix _FILE the file contents are read and returned.
            // when the file doesn't exist, there is an exception, the default will not be used.
            if (!File.Exists(file))
            {
                throw new FileNotFoundException("Cannot read configuration file", file);
            }
            return File.ReadAllText(file);
        }
        
        /// <summary>
        /// Gets a connection string either from a file or from individually configured keys.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="keyPrefix">The key prefix.</param>
        /// <returns>The connection string.</returns>
        public static async Task<string> GetPostgresConnectionString(this IConfiguration configuration, string keyPrefix)
        {
            var file = configuration[$"{keyPrefix}_FILE"];
            if (!string.IsNullOrWhiteSpace(file))
            {
                return await File.ReadAllTextAsync(file);
            }

            var user = configuration.GetValueOrDefault<string>($"{keyPrefix}_USER", "postgres");
            var pass = configuration.GetValueOrDefault<string>($"{keyPrefix}_PASS");
            if (string.IsNullOrWhiteSpace(pass))
            {
                var passFile = configuration.GetValueOrDefault<string>($"{keyPrefix}_PASS_FILE");
                if (!string.IsNullOrWhiteSpace(passFile)) pass = await File.ReadAllTextAsync(passFile);
            }
            var db = configuration.GetValueOrDefault<string>($"{keyPrefix}_DB", "db");
            var host = configuration.GetValueOrDefault<string>($"{keyPrefix}_HOST", "localhost");
            var port = configuration.GetValueOrDefault<string>($"{keyPrefix}_PORT", "5432");

            if (string.IsNullOrWhiteSpace(pass))
            {
                return $"Host={host};Port={port};Database={db};Username={user};";
            }

            return $"Host={host};Port={port};Database={db};Username={user};Password={pass};";
        }
    }
}