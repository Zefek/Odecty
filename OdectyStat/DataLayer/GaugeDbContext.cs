using Microsoft.EntityFrameworkCore;
using OdectyMVC.Business;
using OdectyStat.Entities;

namespace OdectyMVC.DataLayer
{
    public class GaugeDbContext : DbContext
    {
        public GaugeDbContext(DbContextOptions<GaugeDbContext> options) :base(options) { }
        public DbSet<Gauge> Gauge { get; set; }
        public DbSet<GaugeMeasurement> GaugeMeasurement { get; set; }
        public DbSet<GaugeMeasuerementStatistics> GaugeMeasuerementStatistics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Gauge>(opt =>
            {
                opt.Property(k => k.InitialValue).HasPrecision(19, 4);
                opt.Property(k=>k.LastValue).HasPrecision(19, 4);
            });

            modelBuilder.Entity<GaugeMeasurement>(opt =>
            {
                opt.Property(k => k.Value).HasPrecision(19, 4);
                opt.Property(k=>k.CurrentValue).HasPrecision(19, 4);
            });

            modelBuilder.Entity<GaugeMeasuerementStatistics>(opt =>
            {
                opt.Property(k => k.Value).HasPrecision(19, 4);
                opt.Property(k => k.CurrentValue).HasPrecision(19, 4);
            });
        }
    }
}
