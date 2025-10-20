using OdectyStat1.Contracts;
using OdectyStat1.Entities;

namespace OdectyStat1.Business
{
    public class ComputeService2
    {
        private readonly IGaugeContext context;

        public ComputeService2(IGaugeContext context)
        {
            this.context = context;
        }
        public async Task Compute(int gaugeId)
        {
            Console.WriteLine("Výpočet (" + gaugeId.ToString() + ")");
            var days = (await context.MeasurementDayRepository.GetDay(gaugeId)).OrderBy(k => k.MeasurementDateTime).ToList();
            for (int i = 0; i < days.Count; i++)
            {
                GaugeMeasuerementStatistics nextDay = null;
                if (i < days.Count - 1)
                    nextDay = days.ElementAt(i + 1);
                days.ElementAt(i).ComputeToEndDay(nextDay);
            }

            for (var i = days.Min(k => k.MeasurementDateTime.Date); i <= days.Max(k => k.MeasurementDateTime.Date);)
            {
                if (days.FirstOrDefault(k => k.MeasurementDateTime.Date == i.Date) == null)
                {
                    var predecessor = days.First(k => k.MeasurementDateTime.Date == i.AddDays(-1).Date);
                    var successor = FindNext(days, i.AddDays(1));
                    var totalDays = (decimal)Math.Ceiling((successor.MeasurementDateTime - predecessor.MeasurementDateTime).TotalDays);
                    var consumption = (successor.InitialValue - predecessor.CurrentValue) / (totalDays - 1);
                    var value = predecessor.CurrentValue;
                    for (int j = 0; j < totalDays - 1; j++)
                    {
                        var newDay = i.AddDays(j);
                        days.Add(new GaugeMeasuerementStatistics
                        {
                            CurrentValue = value + consumption,
                            Value = consumption,
                            GaugeId = predecessor.GaugeId,
                            MeasurementDateTime = new DateTime(newDay.Year, newDay.Month, newDay.Day, 23, 59, 59)
                        });
                        value += consumption;
                    }
                }
                i = i.AddDays(1);
            }
            //
            context.ExcelProvider.UpdateExcel();
            await context.MeasurementDayRepository.SetStatistics(days);
            Console.WriteLine("Hotovo");
        }

        private GaugeMeasuerementStatistics FindNext(List<GaugeMeasuerementStatistics> measurements, DateTimeOffset from)
        {
            return measurements.FirstOrDefault(k => k.MeasurementDateTime.Date >= from.Date);
        }
    }
}
