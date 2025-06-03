using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat.Dto
{
    public class NewValue
    {
        public int GaugeId { get; set; }
        public decimal Value { get; set; }
        public DateTime Datetime { get; set; }
    }
}
