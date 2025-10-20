namespace OdectyStat1.Contracts
{
    public interface IGaugeContext
    {
        IGaugeRepository GaugeRepository { get; }
        IMeasurementDayRepository MeasurementDayRepository { get; }
        IExcelProvider ExcelProvider { get; }
        IMeasurementStatisticsRepository MeasurementStatisticsRepository { get; }
        IMeasurementRepository MeasurementRepository { get; }
        IHomeAssistantStatisticsRepository HomeAssistantStatisticsRepository { get; }

        IMessageQueue MessageQueue { get; }

        void AddHomeAssistant<TEntity>(TEntity entity);
        void AddRange<TEntity>(ICollection<TEntity> entities);
        Task SaveChangesAsync();
    }
}
