using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat1.Dto;
public class GaugeImageLocation
{
    public string Path { get; set; }
    public string RecognizedFailedFolder { get; set; }
    public string RecognizedSuccessFolder { get; set; }
}
