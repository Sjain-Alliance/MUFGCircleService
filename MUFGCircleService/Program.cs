using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MUFGCircleService
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var configSettings = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                            .Build();

            Log.Logger = new LoggerConfiguration()
                //.MinimumLevel.Debug()
                //.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .ReadFrom.Configuration(configSettings)
                //.Enrich.FromLogContext()
                .WriteTo.File(configSettings["Logging:LogPath"], rollingInterval: RollingInterval.Day)
                .CreateLogger();



            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    EODModel.ConnectionString = configuration.GetConnectionString("SqlConnectionString");
                    services.AddSingleton(configuration.GetSection("MUFGTriggerDetails").Get<MUFGTriggerDetails>());
                    services.AddSingleton(configuration.GetSection("MUFGEmailDetails").Get<MUFGCircleEmailDetails>());
                    services.AddSingleton(configuration.GetSection("FileDetails").Get<FileDetails>());
                    services.AddHostedService<Worker>();
                }).UseSerilog();
    }
}