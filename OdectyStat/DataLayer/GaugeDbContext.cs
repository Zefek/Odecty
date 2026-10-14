using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OdectyStat1.Business;
using OdectyStat1.Entities;

namespace OdectyStat1.DataLayer
{
    public class GaugeDbContext : DbContext
    {
        public GaugeDbContext(DbContextOptions<GaugeDbContext> options) : base(options) { }
        public DbSet<Gauge> Gauge { get; set; }
        public DbSet<GaugeMeasurement> GaugeMeasurement { get; set; }
        public DbSet<GaugeMeasurementStatistics> GaugeMeasurementStatistics { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>()
                .HaveColumnType("timestamp without time zone")
                .HaveConversion<UnspecifiedDateTimeConverter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Gauge>(opt =>
            {
                opt.Property(k => k.InitialValue).HasPrecision(19, 4);
                opt.Property(k => k.LastValue).HasPrecision(19, 4);
                opt.Property(k => k.MaxValuePerHour).HasPrecision(18, 2);
            });

            modelBuilder.Entity<GaugeMeasurement>(opt =>
            {
                opt.Property(k => k.Value).HasPrecision(19, 4);
                opt.Property(k => k.CurrentValue).HasPrecision(19, 4);
                opt.Property(k=>k.Confidence).HasPrecision(19, 4);
            });

            modelBuilder.Entity<GaugeMeasurementStatistics>(opt =>
            {
                opt.Property(k => k.Value).HasPrecision(19, 4);
                opt.Property(k => k.CurrentValue).HasPrecision(19, 4);
            });
        }
    }

    public class UnspecifiedDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UnspecifiedDateTimeConverter()
            : base(v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified), v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified))
        {
        }
    }
}
