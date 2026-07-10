using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat1.Dto;
public class GaugeImageLocation
{
    public required string Path { get; set; }
    public required string RecognizedFailedFolder { get; set; }
    public required string RecognizedSuccessFolder { get; set; }
}
