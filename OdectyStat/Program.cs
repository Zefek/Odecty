// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OdectyStat1.Application;
using OdectyStat1.Contracts;
using OdectyStat1.DataLayer;
using OdectyStat1.DataLayer.Consumers;
using OdectyStat1.Dto;
using Serilog;


Console.WriteLine("Hello, World!");
await Host.CreateDefaultBuilder()
    .UseWindowsService()
    .UseSerilog((hostContext, services, configuration) =>
    {
        configuration.ReadFrom.Configuration(hostContext.Configuration);
    })
    .ConfigureServices((hostContext, services) =>
     {
         services.Configure<OdectySettings>(hostContext.Configuration.GetSection("OdectySettings"));
         services.Configure<GaugeImageLocation>(hostContext.Configuration.GetSection("GaugeImageLocation"));
         services.AddSingleton<RabbitMQProvider>();
         services.AddScoped<IGaugeContext, GaugeContext>();
         services.AddScoped<IGaugeRepository, GaugeRepository>();
         services.AddScoped<IMeasurementDayRepository, MeasurementDayRepository>();
         services.AddScoped<IGaugeService, GaugeService>();
         services.AddScoped<IMessageQueue, MessageQueue>();
         services.AddScoped<IMeasurementRepository, MeasurementRepository>();
         services.AddScoped<IMeasurementStatisticsRepository, MeasurementStatisticsRepository>();
         services.AddScoped<IHomeAssistantStatisticsRepository, HomeAssistantStatisticsRepository>();
         services.AddDbContext<GaugeDbContext>(opt =>
         {
             opt.UseSqlServer(hostContext.Configuration.GetConnectionString("Odecty"));
         })
         .AddDbContext<HomeAssistantDbContext>(opt =>
         {
             opt.UseNpgsql(hostContext.Configuration.GetConnectionString("HomeAssistant"));
         })
         .AddDbContext<DiagDbContext>(opt =>
         {
             opt.UseNpgsql(hostContext.Configuration.GetConnectionString("Diagnostics"));
         })
         .AddHostedService<ConsumerBackgroundService>()
         .AddSingleton<IRabbitMQConsumer, MQClient>()
         .AddSingleton<IRabbitMQConsumer, RecognizedSuccess>()
         .AddSingleton<IRabbitMQConsumer, RecognizedFailed>()
         .AddHostedService<BinaryConsumerBackgroundService>()
         .AddSingleton<IBinaryMessageHandler, HeaterDiagHandler>()
         .AddSingleton<IBinaryMessageHandler, LSSensorDiagHandler>()
         .AddSingleton<IBinaryMessageHandler, GarageDiagHandler>();
     })
    .Build()
    .MigrateAndRunAsync();

static class HostExtensions
{
    public static async Task MigrateAndRunAsync(this IHost host)
    {
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DiagDbContext>();
            await db.Database.MigrateAsync();
        }
        await host.RunAsync();
    }
}