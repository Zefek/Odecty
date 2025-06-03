using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat1.Dto;
public class OdectySettings
{
    public string RabbitMQHost { get; set; }
    public string RabbitMQUsername { get; set; }
    public string RabbitMQPassword { get; set; }
    public string RabbitMQQueue { get; set; }
}
