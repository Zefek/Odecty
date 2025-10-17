// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OdectyStat1;
using OdectyStat1.Application;
using OdectyStat1.Contracts;
using OdectyStat1.DataLayer;
using OdectyStat1.Dto;


Console.WriteLine("Hello, World!");
await Host.CreateDefaultBuilder()
    .UseWindowsService()
     .ConfigureAppConfiguration((hostContext, config) =>
     {
         config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
     })
    .ConfigureServices((hostContext, services) =>
     {
         services.Configure<OdectySettings>(hostContext.Configuration.GetSection("OdectySettings"));
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
         .AddHostedService<MQClient>();
     })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddEventLog(s =>
        {
            s.LogName = "Application";
            s.SourceName = "Odecty";
        });
        if (Environment.UserInteractive)
        {
            logging.AddConsole();
        }
    })
    .Build()
    .RunAsync();