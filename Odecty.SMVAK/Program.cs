// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;

Console.WriteLine("Hello, World!");

var lastDate = DateTime.Now;
var options = new DbContextOptionsBuilder<Odecty.SMVAK.OdectyContext>()
  .UseSqlServer("")
  .Options;
var context = new Odecty.SMVAK.OdectyContext(options);
var lastMeasurement = await context.GaugeMeasurements
    .Where(k => k.LastMeasurementDateTime < lastDate && k.GaugeId == 3 && k.ImagePath != null)
    .Include(k => k.Gauge)
    .OrderByDescending(k => k.LastMeasurementDateTime)
    .FirstAsync();


await Odecty.SMVAK.SMVAKWebForm.SubmitWaterMeterReadingAsync(new()
{
    DeviceNumber = "10418704",
    ReadingDate = DateOnly.FromDateTime(lastMeasurement.LastMeasurementDateTime),
    Value = (int)Math.Truncate(lastMeasurement.CurrentValue),
    ReasonCode = "14", // běžné odečtení
    CustomerNumber = "",
    Filename = ""+ lastMeasurement.ImagePath!,
    Name = "",
    Email = ""
});



