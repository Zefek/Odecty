using Microsoft.EntityFrameworkCore;
using OdectyStat1.Business;

namespace OdectyStat1.DataLayer;

public class DiagDbContext : DbContext
{
    public DiagDbContext(DbContextOptions<DiagDbContext> options) : base(options)
    {
    }

    public DbSet<HeaterDiagnostic> HeaterDiagnostics { get; set; }
    public DbSet<LSSensorDiagnostic> LSSensorDiagnostics { get; set; }
    public DbSet<GarageDiagnostic> GarageDiagnostics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<HeaterDiagnostic>(entity =>
        {
            entity.ToTable("heater_diagnostics");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.UptimeMinutes).HasColumnName("uptime_minutes");
            entity.Property(e => e.FreeRam).HasColumnName("free_ram");
            entity.Property(e => e.WifiReconnects).HasColumnName("wifi_reconnects");
            entity.Property(e => e.MqttReconnects).HasColumnName("mqtt_reconnects");
            entity.Property(e => e.SensorErrors).HasColumnName("sensor_errors");
            entity.Property(e => e.ResetReason).HasColumnName("reset_reason");
            entity.Property(e => e.LoopMaxMs).HasColumnName("loop_max_ms");

            entity.HasIndex(e => e.Timestamp, "ix_heater_diagnostics_timestamp");
        });

        modelBuilder.Entity<LSSensorDiagnostic>(entity =>
        {
            entity.ToTable("ls_sensor_diagnostics");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.UptimeMinutes).HasColumnName("uptime_minutes");
            entity.Property(e => e.FreeRam).HasColumnName("free_ram");
            entity.Property(e => e.WifiReconnects).HasColumnName("wifi_reconnects");
            entity.Property(e => e.MqttFailCount).HasColumnName("mqtt_fail_count");
            entity.Property(e => e.ResetReason).HasColumnName("reset_reason");
            entity.Property(e => e.LoopMaxMs).HasColumnName("loop_max_ms");

            entity.HasIndex(e => e.Timestamp, "ix_ls_sensor_diagnostics_timestamp");
        });

        modelBuilder.Entity<GarageDiagnostic>(entity =>
        {
            entity.ToTable("garage_diagnostics");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.UptimeMinutes).HasColumnName("uptime_minutes");
            entity.Property(e => e.FreeRam).HasColumnName("free_ram");
            entity.Property(e => e.WifiReconnects).HasColumnName("wifi_reconnects");
            entity.Property(e => e.MqttReconnects).HasColumnName("mqtt_reconnects");
            entity.Property(e => e.SensorErrors).HasColumnName("sensor_errors");
            entity.Property(e => e.ResetReason).HasColumnName("reset_reason");
            entity.Property(e => e.LoopMaxMs).HasColumnName("loop_max_ms");
            entity.Property(e => e.DoorCycles).HasColumnName("door_cycles");

            entity.HasIndex(e => e.Timestamp, "ix_garage_diagnostics_timestamp");
        });
    }
}
