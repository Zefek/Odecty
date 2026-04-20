using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odecty.SMVAK;
internal class WaterMeterReading
{
    public string DeviceNumber { get; set; }
    public DateOnly ReadingDate { get; set; }
    public int Value { get; set; }
    public string ReasonCode { get; set; }
    public string CustomerNumber { get; set; }
    public string Filename { get; internal set; }
    public string Name { get; internal set; }
    public string Email { get; internal set; }
}
