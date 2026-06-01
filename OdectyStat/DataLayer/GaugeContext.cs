using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer
{
    public class GaugeContext : IGaugeContext
    {
        private readonly GaugeDbContext context;
        private readonly HomeAssistantDbContext homeAssistantDbContext;

        public GaugeContext(IGaugeRepository gaugeRepository,
            GaugeDbContext context,
            IMeasurementDayRepository measurementDayRepository,
            IMeasurementStatisticsRepository measurementStatisticsRepository,
            IMeasurementRepository measurementRepository,
            IHomeAssistantStatisticsRepository homeAssistantStatisticsRepository,
            HomeAssistantDbContext homeAssistantDbContext,
            IMessageQueue messageQueue)
        {
            GaugeRepository = gaugeRepository;
            this.context = context;
            MeasurementDayRepository = measurementDayRepository;
            MeasurementStatisticsRepository = measurementStatisticsRepository;
            MeasurementRepository = measurementRepository;
            HomeAssistantStatisticsRepository = homeAssistantStatisticsRepository;
            this.homeAssistantDbContext = homeAssistantDbContext;
            MessageQueue = messageQueue;
        }

        public IGaugeRepository GaugeRepository { get; }
        public IMeasurementDayRepository MeasurementDayRepository { get; }

        public IMeasurementStatisticsRepository MeasurementStatisticsRepository { get; }

        public IMeasurementRepository MeasurementRepository { get; }

        public IHomeAssistantStatisticsRepository HomeAssistantStatisticsRepository { get; }
        public IMessageQueue MessageQueue { get; }

        public void AddHomeAssistant<TEntity>(TEntity entity)
        {
            if (entity != null)
            {
                homeAssistantDbContext.Add(entity);
            }
        }

        public void AddRange<TEntity>(ICollection<TEntity> entities)
        {
            foreach (TEntity entity in entities)
            {
                if (entity != null)
                {
                    context.Add(entity);
                }
            }
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
            await homeAssistantDbContext.SaveChangesAsync();
        }
    }
}
