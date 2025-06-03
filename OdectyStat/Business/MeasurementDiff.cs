using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat.Business
{
    public class MeasurementDiff
    {
        public decimal Consumption { get; internal set; }
        public TimeSpan TotalDateTime { get; internal set; }
    }
}
