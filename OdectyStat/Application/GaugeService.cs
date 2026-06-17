using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OdectyStat1.Business;
using OdectyStat1.Contracts;
using OdectyStat1.Dto;

namespace OdectyStat1.Application
{
    public class GaugeService : IGaugeService
    {
        private readonly IGaugeContext context;
        private readonly IOptions<GaugeImageLocation> options;
        private readonly ILogger<GaugeService> logger;

        public GaugeService(IGaugeContext context, IOptions<GaugeImageLocation> options, ILogger<GaugeService> logger)
        {
            this.context = context;
            this.options = options;
            this.logger = logger;
        }

        public async Task AddNewValue(NewValue newValue)
        {
            Console.WriteLine("Add new value " + newValue.GaugeId);
            var gauge = await context.GaugeRepository.GetGauge(newValue.GaugeId);
            if (gauge == null)
            {
                logger.LogError("Gauge {gaugeId} not found.", newValue.GaugeId);
                throw new Exception($"Gauge {newValue.GaugeId} not found.");
            }
            gauge.SetNewValue(newValue.Value, newValue.Datetime);
            await context.SaveChangesAsync();
            await context.MessageQueue.Publish(new { gaugeId = newValue.GaugeId, value = gauge.LastValue }, MessageQueueRoutingKeys.Odecty_Gauge_Lastvaluechanged);
            var service = new ComputeService3(context);
            var result = await service.Compute(newValue.GaugeId);
            context.AddRange(result);

            if (gauge.HomeassistantId.HasValue)
            {
                var homeStatistics = await context.HomeAssistantStatisticsRepository.GetForGauge(gauge.HomeassistantId.Value);
                var shortTerm = await context.HomeAssistantStatisticsRepository.GetForStatisticsShortTerm(gauge.HomeassistantId.Value);
                foreach (var r in result)
                {
                    var time = new DateTimeOffset(r.MeasurementDateTime.Date.AddDays(1)).ToUnixTimeSeconds();
                    var timeStartDay = new DateTimeOffset(r.MeasurementDateTime.Date).ToUnixTimeSeconds();
                    var lastDayRecord = homeStatistics.Where(k => k.StartTs >= timeStartDay && k.StartTs < time).OrderByDescending(k => k.StartTs).FirstOrDefault();
                    var lastDayShort = shortTerm.Where(k => k.StartTs >= timeStartDay && k.StartTs < time).OrderByDescending(k => k.StartTs).FirstOrDefault();
                    if (lastDayRecord != null)
                    {
                        lastDayRecord.State = Math.Round((double)r.Value, 4);
                    }
                    else
                    {
                        lastDayRecord = new Statistic
                        {
                            StartTs = new DateTimeOffset(r.MeasurementDateTime.Date.AddHours(20)).ToUnixTimeSeconds(),
                            CreatedTs = new DateTimeOffset(r.MeasurementDateTime.Date.AddHours(20)).ToUnixTimeSeconds(),
                            State = (double)r.Value,
                            MetadataId = gauge.HomeassistantId.Value
                        };
                        homeStatistics.Add(lastDayRecord);
                        context.AddHomeAssistant(lastDayRecord);
                    }
                    if (lastDayShort != null)
                    {
                        lastDayShort.State = Math.Round((double)r.Value, 4);
                    }
                }
                double sum = 0;
                foreach (var s in homeStatistics.OrderBy(k => k.StartTs))
                {
                    sum += s.State ?? 0;
                    s.Sum = Math.Round(sum, 4);
                }

                foreach (var s in shortTerm.OrderByDescending(k => k.StartTs))
                {
                    s.Sum = Math.Round(sum, 4);
                    sum -= s.State ?? 0;
                }
            }
            await context.SaveChangesAsync();

            //context.ExcelProvider.UpdateExcel();
        }

        public async Task AddIncrement(int gaugeId, decimal increment, DateTime datetime)
        {
            var gauge = await context.GaugeRepository.GetGauge(gaugeId);
            gauge?.AddIncrement(increment, datetime);
            await context.SaveChangesAsync();
        }

        public void GaugeRecognizedFailed(int gaugeId, string imagePath)
        {
            logger.LogInformation("Recognition failed for gauge {gaugeId} with image {imagePath}", gaugeId, imagePath);
            var dateFolder = File.GetCreationTime(Path.Combine(string.Format(options.Value.Path, gaugeId), imagePath)).ToString("yyyy-MM-dd");
            MoveFile(gaugeId, imagePath, imagePath, dateFolder, false);
        }

        public async Task GaugeRecognizedSucceeded(int gaugeId, string imagePath, decimal value, DateTime dateTime, decimal? confidence)
        {
            logger.LogInformation("Recognition succeeded for gauge {gaugeId} with image {imagePath} and value {value}", gaugeId, imagePath, value);
            var gauge = await context.GaugeRepository.GetGauge(gaugeId);
            if (gauge == null)
            {
                logger.LogError("Gauge {gaugeId} not found", gaugeId);
                throw new Exception($"Gauge {gaugeId} not found.");
            }
            var localDateTime = dateTime.ToLocalTime();
            bool valid = false;
            if (gauge.LastMeasurement != null)
            {
                var prevValue = gauge.LastMeasurement.CurrentValue;
                if (gauge.LastMeasurement.CurrentValue == value)
                {
                    valid = true;
                }
                else
                {
                    var timeDiff = localDateTime - gauge.LastMeasurement.LastMeasurementDateTime;
                    if (timeDiff.TotalHours > 0 && gauge.MaxValuePerHour.HasValue)
                    {
                        var maxAllowedIncrement = gauge.MaxValuePerHour.Value * (decimal)timeDiff.TotalHours;
                        if (value - prevValue <= maxAllowedIncrement && value - prevValue >= 0)
                        {
                            valid = true;
                        }
                        else
                        {
                            logger.LogWarning("Recognized value {value} exceeds maximum allowed increment {maxAllowedIncrement} for gauge {gaugeId}. Marking as failed.", value, maxAllowedIncrement, gaugeId);
                            valid = false;

                            decimal prevInt = Math.Truncate(prevValue);

                            decimal newInt = Math.Truncate(value);
                            decimal newDec = value - newInt;

                            for (int inc = 0; inc <= 2; inc++)
                            {
                                decimal candidate = (prevInt + inc) + newDec;
                                decimal diff = candidate - prevValue;

                                if (diff >= 0 && diff <= maxAllowedIncrement)
                                {
                                    value = decimal.Round(candidate, 4);
                                    valid = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else if (gauge.LastValue > value)
            {
                logger.LogWarning("Recognized value {value} is less than last value {lastValue} for gauge {gaugeId}. Marking as failed.", value, gauge.LastValue, gaugeId);
                valid = false;
            }
            else
            {
                valid = true;
            }
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imagePath);
            var extension = Path.GetExtension(imagePath);
            var newFileName = fileNameWithoutExtension + "_" + value.ToString().Replace(".", "-") + extension;
            var sourcePath = Path.Combine(string.Format(options.Value.Path, gaugeId), imagePath);
            var dateFolder = File.GetCreationTime(sourcePath).ToString("yyyy-MM-dd");
            // Do DB ukládáme relativní cestu včetně datové složky, aby se snímek dal
            // dohledat i když hodnota stojí přes hranici dne (jiná složka než datum změny).
            var relativeImagePath = Path.Combine(dateFolder, newFileName);
            if (valid)
            {
                logger.LogInformation("Updating gauge {gaugeId} with new value {value}", gaugeId, value);
                gauge.SetNewValue(value, localDateTime, relativeImagePath, confidence);
                await context.SaveChangesAsync();
                await context.MessageQueue.MQTTPublish(JsonConvert.SerializeObject(new { Value = value.ToString().Replace(",", "."), Confidence = confidence }), MessageQueueRoutingKeys.WatermeterState);
                await context.MessageQueue.Publish(new { gaugeId, value = gauge.LastValue }, MessageQueueRoutingKeys.Odecty_Gauge_Lastvaluechanged);
                MoveFile(gaugeId, imagePath, newFileName, dateFolder, true);
            }
            else
            {
                if (gauge.LastMeasurement != null)
                {
                    gauge.LastMeasurement.LastMeasurementDateTime = localDateTime;
                    await context.SaveChangesAsync();
                }
                logger.LogWarning("Could not validate recognized value {value} for gauge {gaugeId}. Marking as failed.", value, gaugeId);
                MoveFile(gaugeId, imagePath, newFileName, dateFolder, false);
            }
        }

        private void MoveFile(int gaugeId, string oldPath, string newPath, string dateFolder, bool success)
        {
            var targetFolder = success ? options.Value.RecognizedSuccessFolder : options.Value.RecognizedFailedFolder;
            targetFolder = string.Format(targetFolder, gaugeId);
            targetFolder = Path.Combine(targetFolder, dateFolder);
            logger.LogInformation("Moving file {imagePath} to folder {targetFolder}", oldPath, targetFolder);
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }
            File.Move(Path.Combine(string.Format(options.Value.Path, gaugeId), oldPath), Path.Combine(targetFolder, newPath));
        }
    }
}
