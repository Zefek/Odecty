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
    public DbSet<FileDiagnostic> FileDiagnostics { get; set; }
    public DbSet<TransferDiagnostic> TransferDiagnostics { get; set; }

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
            entity.Property(e => e.Rssi).HasColumnName("rssi");

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
            entity.Property(e => e.Rssi).HasColumnName("rssi");

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
            entity.Property(e => e.Rssi).HasColumnName("rssi");

            entity.HasIndex(e => e.Timestamp, "ix_garage_diagnostics_timestamp");
        });

        modelBuilder.Entity<FileDiagnostic>(entity =>
        {
            entity.ToTable("file_diagnostics");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasColumnType("numeric(20,0)");
            entity.Property(e => e.GaugeId).HasColumnName("gauge_id");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.FilePath).HasColumnName("file_path");
            entity.Property(e => e.Success).HasColumnName("success");
            entity.Property(e => e.RecognizedValue).HasColumnName("recognized_value");
            entity.Property(e => e.CorrectedValue).HasColumnName("corrected_value");
            entity.Property(e => e.Confidence).HasColumnName("confidence");

            entity.HasIndex(e => e.CorrelationId, "ix_file_diagnostics_correlation_id");
            entity.HasIndex(e => e.Timestamp, "ix_file_diagnostics_timestamp");
        });

        modelBuilder.Entity<TransferDiagnostic>(entity =>
        {
            entity.ToTable("transfer_diagnostics");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasColumnType("numeric(20,0)");
            entity.Property(e => e.GaugeId).HasColumnName("gauge_id");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.SchemaVer).HasColumnName("schema_ver");
            entity.Property(e => e.ImgSize).HasColumnName("img_size");
            entity.Property(e => e.BytesSent).HasColumnName("bytes_sent");
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.TryCount).HasColumnName("try_count");
            entity.Property(e => e.Success).HasColumnName("success");
            entity.Property(e => e.HttpCode).HasColumnName("http_code");
            entity.Property(e => e.Rssi).HasColumnName("rssi");

            entity.HasIndex(e => e.CorrelationId, "ix_transfer_diagnostics_correlation_id");
            entity.HasIndex(e => e.Timestamp, "ix_transfer_diagnostics_timestamp");
        });
    }
}
