using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odecty.SMVAK;
internal class OdectyContext : DbContext
{
    public OdectyContext(DbContextOptions<OdectyContext> options) : base(options)
    {
    }

    public DbSet<Gauge> Gauges { get; set; }
    public DbSet<GaugeMeasurement> GaugeMeasurements { get; set; }


}
