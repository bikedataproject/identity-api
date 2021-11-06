using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BikeDataProject.Identity.Db;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BikeDataProject.Identity.API.Data.Initial
{
    internal static class InitialData
    {
        private const string IdentityApiResource = "identity";

        /// <summary>
        /// Initializes the database with minimal data.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public static async Task Initialize(this IServiceProvider serviceProvider)
        {
            // initialize database and migrations if needed.
            using var scope = serviceProvider.CreateScope();
            Log.Information("Initializing database...");
            using var serviceScope =
                scope.ServiceProvider.GetService<IServiceScopeFactory>().CreateScope();
            await serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();

            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            await context.Database.MigrateAsync();

            // adds clients that aren't there yet.
            foreach (var client in InitialData.GetClients())
            {
                var dbClients = context.Clients.SingleOrDefault(c => c.ClientId == client.ClientId);
                if (dbClients != null) continue;

                Log.Information("Adding client {ClientId} to database", client.ClientId);
                context.Clients.Add(client.ToEntity());
            }

            await context.SaveChangesAsync();

            // adds identity resources that aren't there yet.
            foreach (var resource in InitialData.GetIdentityResources())
            {
                var dbResource = context.IdentityResources.SingleOrDefault(c => c.Name == resource.Name);
                if (dbResource != null) continue;

                Log.Information("Adding identity resource {ResourceName} to database", resource.Name);
                context.IdentityResources.Add(resource.ToEntity());
            }

            await context.SaveChangesAsync();

            // adds api resources that aren't there yet.
            foreach (var apiResource in InitialData.GetApiResources())
            {
                var dbResource = context.ApiResources.SingleOrDefault(c => c.Name == apiResource.Name);
                if (dbResource != null) continue;

                Log.Information("Adding API resource {ApiResourceName} to database", apiResource.Name);
                context.ApiResources.Add(apiResource.ToEntity());
            }

            await context.SaveChangesAsync();

            // do application db context.
            var identityDbContext =
                serviceScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await identityDbContext.Database.MigrateAsync();

            await identityDbContext.SaveChangesAsync();
        }

        private static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new (IdentityApiResource, "Identity API")
            };
        }

        private static IEnumerable<Client> GetClients()
        {
            return new List<Client>();
        }

        private static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new("roles",
                    "Roles",
                    new[] { "role" })
            };
        }
    }
}