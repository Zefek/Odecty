using Microsoft.EntityFrameworkCore;
using OdectyMVC.Business;
using OdectyMVC.Contracts;
using OdectyStat.Contracts;
using OdectyStat1.Contracts;
using OdectyStat1.DataLayer;

namespace OdectyMVC.DataLayer
{
    public class GaugeContext : IGaugeContext
    {
        private readonly GaugeDbContext context;
        private readonly HomeAssistantDbContext homeAssistantDbContext;

        public GaugeContext(IGaugeRepository gaugeRepository,
            GaugeDbContext context,
            IMeasurementDayRepository measurementDayRepository,
            //IExcelProvider excelProvider,
            IMeasurementStatisticsRepository measurementStatisticsRepository,
            IMeasurementRepository measurementRepository,
            IHomeAssistantStatisticsRepository homeAssistantStatisticsRepository,
            HomeAssistantDbContext homeAssistantDbContext,
            IMessageQueue messageQueue)
        {
            GaugeRepository=gaugeRepository;
            this.context=context;
            MeasurementDayRepository=measurementDayRepository;
            //ExcelProvider=excelProvider;
            MeasurementStatisticsRepository=measurementStatisticsRepository;
            MeasurementRepository=measurementRepository;
            HomeAssistantStatisticsRepository=homeAssistantStatisticsRepository;
            this.homeAssistantDbContext=homeAssistantDbContext;
            MessageQueue = messageQueue;
        }

        public IGaugeRepository GaugeRepository { get; }
        public IMeasurementDayRepository MeasurementDayRepository { get; }
        public IExcelProvider ExcelProvider { get; }

        public IMeasurementStatisticsRepository MeasurementStatisticsRepository { get; }

        public IMeasurementRepository MeasurementRepository { get; }

        public IHomeAssistantStatisticsRepository HomeAssistantStatisticsRepository { get; }
        public IMessageQueue MessageQueue { get; }

        public void AddHomeAssistant<TEntity>(TEntity entity)
        {
            homeAssistantDbContext.Add(entity);
        }

        public void AddRange<TEntity>(ICollection<TEntity> entities)
        { 
            foreach (TEntity entity in entities)
            {
                context.Add(entity);
            }
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
            await homeAssistantDbContext.SaveChangesAsync();
        }
    }
}
