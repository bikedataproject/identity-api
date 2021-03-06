using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BikeDataProject.Identity.Db
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // EF Core uses this method at design time to access the DbContext
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(x => x.AddJsonFile("appsettings.json"))
                .ConfigureServices(async (hc, s) =>
                {
                    var connectionString =
                        await hc.Configuration.GetPostgresConnectionString("IDENTITY_DB");
                    
                    s.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connectionString));
                    
                    s.AddHostedService<Startup>();
                });
        }
        
        public class Startup : IHostedService
        {
            public Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}