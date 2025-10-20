using OdectyStat1.Business;
using OdectyStat1.Entities;
namespace OdectyStat1
{
    public class ComputeService
    {
        public Task Compute(Gauge gauge)
        {
            Console.WriteLine("Počítám GaugeId (" + gauge.Id.ToString() + ") " + gauge.Description);
            var initialValue = gauge.InitialValue;// 21135.1680;

            //Jen poslední z daného dne
            var filtered = new List<GaugeMeasurement>();
            foreach (var measurement in gauge.Measurements.GroupBy(k => k.MeasurementDateTime.Date))
            {
                filtered.Add(measurement.OrderByDescending(k => k.MeasurementDateTime).First());
            }

            //Dopočítáme to do 23:59:59
            filtered = filtered.OrderBy(k => k.MeasurementDateTime).ToList();
            var newMeasurement = new List<GaugeMeasurement>();
            for (int i = 0; i < filtered.Count - 1; i++)
            {
                var measureDay = filtered.ElementAt(i).MeasurementDateTime;
                var minutesToEndDay = (decimal)(new DateTime(measureDay.Year, measureDay.Month, measureDay.Day, 23, 59, 59) - measureDay).TotalMinutes;
                var diff = filtered.ElementAt(i + 1).CurrentValue - filtered.ElementAt(i).CurrentValue;
                var timeDiff = (decimal)(filtered.ElementAt(i + 1).MeasurementDateTime - filtered.ElementAt(i).MeasurementDateTime).TotalMinutes;
                var valueToAdd = (diff / timeDiff) * minutesToEndDay;
                newMeasurement.Add(new GaugeMeasurement
                {
                    CurrentValue = filtered.ElementAt(i).CurrentValue + valueToAdd,
                    GaugeId = filtered.ElementAt(i).GaugeId,
                    MeasurementDateTime = new DateTime(measureDay.Year, measureDay.Month, measureDay.Day, 23, 59, 59)
                });
            }

            newMeasurement.Add(new GaugeMeasurement
            {
                CurrentValue = filtered.OrderByDescending(k => k.MeasurementDateTime).First().CurrentValue,
                GaugeId = filtered.ElementAt(0).GaugeId,
                MeasurementDateTime = DateTime.Parse(filtered.OrderByDescending(k => k.MeasurementDateTime).First().MeasurementDateTime.ToString("yyyy-MM-dd HH:mm:ss"))
            });

            //Doplníme chybějící dny
            newMeasurement = newMeasurement.OrderBy(k => k.MeasurementDateTime).ToList();
            for (DateTime i = newMeasurement.Min(k => k.MeasurementDateTime.Date); i <= newMeasurement.Max(k => k.MeasurementDateTime.Date);)
            {
                if (newMeasurement.FirstOrDefault(k => k.MeasurementDateTime.Date == i.Date) == null)
                {
                    var predecessor = newMeasurement.First(k => k.MeasurementDateTime.Date == i.AddDays(-1).Date);
                    var successor = FindNext(newMeasurement, i.AddDays(1));
                    var days = (decimal)Math.Ceiling((successor.MeasurementDateTime - predecessor.MeasurementDateTime).TotalDays);
                    var consumption = (successor.CurrentValue - predecessor.CurrentValue) / days;
                    var value = predecessor.CurrentValue;
                    for (int j = 0; j < days - 1; j++)
                    {
                        var newDay = i.AddDays(j);
                        newMeasurement.Add(new GaugeMeasurement
                        {
                            CurrentValue = value + consumption,
                            GaugeId = predecessor.GaugeId,
                            MeasurementDateTime = new DateTime(newDay.Year, newDay.Month, newDay.Day, 23, 59, 59)
                        });
                        value += consumption;
                    }
                }
                i = i.AddDays(1);
            }
            var ordered = newMeasurement.OrderBy(k => k.MeasurementDateTime);
            for (int i = 1; i < ordered.Count(); i++)
            {
                ordered.ElementAt(i).Value = ordered.ElementAt(i).CurrentValue - ordered.ElementAt(i - 1).CurrentValue;
            }
            var stat = new List<GaugeMeasuerementStatistics>();
            foreach (var order in ordered)
            {
                stat.Add(new GaugeMeasuerementStatistics
                {
                    CurrentValue = (decimal)order.CurrentValue,
                    GaugeId = order.GaugeId,
                    MeasurementDateTime = order.MeasurementDateTime,
                    Value = (decimal)order.Value
                });
            }
            gauge.SetStatistics(stat);
            Console.WriteLine("Výpočet dokončen");
            return Task.CompletedTask;
        }

        private GaugeMeasurement FindNext(List<GaugeMeasurement> measurements, DateTimeOffset from)
        {
            return measurements.FirstOrDefault(k => k.MeasurementDateTime.Date >= from.Date);
        }
    }
}
