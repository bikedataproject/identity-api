using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BikeDataProject.Identity.API.Data.Initial;
using BikeDataProject.Identity.API.Controllers.Integrations.Fitbit;
using BikeDataProject.Identity.API.Data;
using BikeDataProject.Identity.API.Extensions;
using BikeDataProject.Identity.API.Policies;
using BikeDataProject.Identity.API.Services;
using BikeDataProject.Identity.API.Services.Mailjet;
using BikeDataProject.Identity.Db;
using Fitbit.Api.Portable;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSwag;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace BikeDataProject.Identity.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // hardcode configuration before the configured logging can be bootstrapped.
            var logFile = Path.Combine("logs", "boot-log-.txt");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Verbose)
                .Enrich.FromLogContext()
                .WriteTo.File(new JsonFormatter(), logFile, rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                // create host.
                var host = CreateHostBuilder(args).Build();

                // initialize db.
                await host.Services.Initialize();

                // run!
                await host.RunAsync();
            }
            catch (Exception e)
            {
                Log.Logger.Fatal(e, "Unhandled exception");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true);

            // get deploy time setting.
            var (deployTimeSettings, envVarPrefix) = configurationBuilder.GetDeployTimeSettings();

            return WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    Log.Information("Env: {EnvironmentName}",
                        hostingContext.HostingEnvironment.EnvironmentName);

                    config.AddJsonFile(deployTimeSettings, true, true);
                    Log.Logger.Debug("Env configuration prefix: {EnvVarPrefix}", envVarPrefix);
                    config.AddEnvironmentVariables((c) => { c.Prefix = envVarPrefix; });
                })
                .ConfigureServices(async (hostingContext, services) =>
                {
                    try
                    {
                        services.AddCors(options =>
                        {
                            options.AddPolicy(name: "CORS",
                                builder => { builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); });
                        });

                        // setup database.
                        var connectionString =
                            await hostingContext.Configuration.GetPostgresConnectionString("IDENTITY_DB");
                        var migrationsAssembly = typeof(Program).GetTypeInfo().Assembly.GetName().Name;
                        services
                            .AddEntityFrameworkNpgsql()
                            .AddDbContext<ApplicationDbContext>(options =>
                                options.UseNpgsql(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));

                        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                            {
                                options.Password.RequireDigit = false;
                                options.Password.RequireLowercase = false;
                                options.Password.RequireUppercase = false;
                                options.Password.RequiredUniqueChars = 0;
                                options.Password.RequireNonAlphanumeric = false;

                                options.SignIn.RequireConfirmedEmail = true;
                                options.User.RequireUniqueEmail = true;
                            })
                            .AddEntityFrameworkStores<ApplicationDbContext>()
                            .AddDefaultTokenProviders();

                        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>,
                            AdditionalUserClaimsPrincipalFactory>();

                        services.AddSingleton<IAuthorizationHandler, IsAdminHandler>();
                        services.AddAuthorization(options =>
                        {
                            options.AddPolicy("IsAdmin",
                                policyIsAdminRequirement =>
                                {
                                    policyIsAdminRequirement.Requirements.Add(new IsAdminRequirement());
                                });
                        });

                        // Add application services.
                        services.AddSingleton(new MailjetConfiguration()
                        {
                            ApiKey = hostingContext.Configuration.GetValueOrDefault("MAILJET_APIKEY"),
                            ApiSecret = hostingContext.Configuration.GetValueOrDefault("MAILJET_APISECRET"),
                            FitbitTemplateId = int.Parse(hostingContext.Configuration.GetValueOrDefault("MAILJET_TEMPLATE_FITBIT"))
                        });
                        services.AddTransient<IEmailSender, MailjetEmailSender>();

                        services.AddRouting(options => options.LowercaseUrls = true);
                        services.AddControllers();
                        services.AddOpenApiDocument(); // add OpenAPI v3 document

                        // configure identity server with in-memory stores, keys, clients and scopes
                        services.AddIdentityServer()
                            .AddDeveloperSigningCredential()
                            .AddConfigurationStore(options =>
                            {
                                options.ConfigureDbContext = builder =>
                                    builder.UseNpgsql(connectionString,
                                        sql => sql.MigrationsAssembly(migrationsAssembly));
                            })
                            // this adds the operational data from DB (codes, tokens, consents)
                            .AddOperationalStore(options =>
                            {
                                options.ConfigureDbContext = builder =>
                                    builder.UseNpgsql(connectionString,
                                        sql => sql.MigrationsAssembly(migrationsAssembly));

                                // this enables automatic token cleanup. this is optional.
                                options.EnableTokenCleanup = true;
                                options.TokenCleanupInterval = 30;
                            })
                            .AddAspNetIdentity<ApplicationUser>();

                        services.ConfigureApplicationCookie(options =>
                        {
                            options.AccessDeniedPath = "/Identity/Account/AccessDenied";     
                            options.Cookie.Name = "BikeDataProjectAuthenticationCookie";
                            options.Cookie.HttpOnly = true;
                            options.Cookie.SameSite = SameSiteMode.None;
                            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                            options.LoginPath = "/login";
                            // ReturnUrlParameter requires 
                            //using Microsoft.AspNetCore.Authentication.Cookies;
                            options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
                            options.SlidingExpiration = true;
                        });

                        // read/parse fitbit configurations.
                        var fitbitCredentials = new FitbitAppCredentials()
                        {
                            ClientId = hostingContext.Configuration.GetValueOrDefault("FITBIT_CLIENT_ID"),
                            ClientSecret = hostingContext.Configuration.GetValueOrDefault("FITBIT_CLIENT_SECRET")
                        };
                        services.AddSingleton(new FitbitAccountControllerSettings()
                        {
                            FitbitAppCredentials = fitbitCredentials
                        });
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Fatal(e, "Unhandled exception during configuration");
                        throw;
                    }
                }).Configure((app) =>
                {
                    // add support for the proxy headers.
                    app.UseForwardedNGINXHeaders();

                    app.UseStaticFiles();

                    app.UseCors("CORS");
                    
                    app.UseRouting();

                    app.UseAuthorization();
                    // app.UseAuthentication(); // not needed, since UseIdentityServer adds the authentication middleware
                    app.UseIdentityServer();

                    // Register the Swagger generator middleware
                    app.UseOpenApi(settings =>
                    {
                        settings.PostProcess = (document, req) =>
                        {
                            document.Info.Version = "v1";
                            document.Info.Title = "Identity API";
                            document.Info.Description =
                                "The Identity API.";
                            document.Info.TermsOfService = "None";
                            document.Info.Contact = new OpenApiContact()
                            {
                                Name = "Open Knowledge Belgium VZW",
                                Email = "bikedataproject@openknowledge.be",
                                Url = "https://www.bikedataproject.org"
                            };
                            document.BasePath = req.PathBase;
                            document.Host = req.Host.Value;
                        };
                    });
                    app.UseSwaggerUi3(config =>
                    {
                        config.ValidateSpecification = false;
                        config.TransformToExternalPath = (internalUiRoute, request) =>
                        {
                            // The header X-External-Path is set in the nginx.conf file
                            var externalPath = request.PathBase.Value;
                            if (externalPath != null && externalPath.EndsWith("/"))
                            {
                                externalPath = externalPath.Substring(0, externalPath.Length - 1);
                            }

                            if (!internalUiRoute.StartsWith(externalPath))
                            {
                                return externalPath + internalUiRoute;
                            }

                            return internalUiRoute;
                        };
                    });

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                })
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .Enrich.FromLogContext());
        }
    }
}