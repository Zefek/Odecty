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
    public DbSet<DeviceDiagnostic> DeviceDiagnostics { get; set; }
    public DbSet<CameraConfig> CameraConfigs { get; set; }

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

        modelBuilder.Entity<DeviceDiagnostic>(entity =>
        {
            entity.ToTable("device_diagnostics");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GaugeId).HasColumnName("gauge_id");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.SchemaVer).HasColumnName("schema_ver");
            entity.Property(e => e.UptimeSeconds).HasColumnName("uptime_seconds");
            entity.Property(e => e.FwVersion).HasColumnName("fw_version");
            entity.Property(e => e.FreeHeapKb).HasColumnName("free_heap_kb");
            entity.Property(e => e.MinFreeHeapKb).HasColumnName("min_free_heap_kb");
            entity.Property(e => e.MaxAllocKb).HasColumnName("max_alloc_kb");
            entity.Property(e => e.WifiReconnects).HasColumnName("wifi_reconnects");
            entity.Property(e => e.CaptureCount).HasColumnName("capture_count");
            entity.Property(e => e.SendFailures).HasColumnName("send_failures");
            entity.Property(e => e.TlsErrors).HasColumnName("tls_errors");
            entity.Property(e => e.OtaFailures).HasColumnName("ota_failures");
            entity.Property(e => e.LoopMaxMs).HasColumnName("loop_max_ms");
            entity.Property(e => e.CameraErrors).HasColumnName("camera_errors");
            entity.Property(e => e.ResetReason).HasColumnName("reset_reason");
            entity.Property(e => e.Rssi).HasColumnName("rssi");
            entity.Property(e => e.CfgHash).HasColumnName("cfg_hash");

            entity.HasIndex(e => e.GaugeId, "ix_device_diagnostics_gauge_id");
            entity.HasIndex(e => e.Timestamp, "ix_device_diagnostics_timestamp");
        });

        modelBuilder.Entity<CameraConfig>(entity =>
        {
            entity.ToTable("camera_config");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GaugeId).HasColumnName("gauge_id");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.SchemaVer).HasColumnName("schema_ver");
            entity.Property(e => e.FwVersion).HasColumnName("fw_version");
            entity.Property(e => e.Framesize).HasColumnName("framesize");
            entity.Property(e => e.Quality).HasColumnName("quality");
            entity.Property(e => e.AecValue).HasColumnName("aec_value");
            entity.Property(e => e.ExposureCtrl).HasColumnName("exposure_ctrl");
            entity.Property(e => e.GainCtrl).HasColumnName("gain_ctrl");
            entity.Property(e => e.AgcGain).HasColumnName("agc_gain");
            entity.Property(e => e.AeLevel).HasColumnName("ae_level");
            entity.Property(e => e.Brightness).HasColumnName("brightness");
            entity.Property(e => e.Contrast).HasColumnName("contrast");
            entity.Property(e => e.Saturation).HasColumnName("saturation");
            entity.Property(e => e.Whitebal).HasColumnName("whitebal");
            entity.Property(e => e.AwbGain).HasColumnName("awb_gain");
            entity.Property(e => e.WbMode).HasColumnName("wb_mode");
            entity.Property(e => e.SpecialEffect).HasColumnName("special_effect");
            entity.Property(e => e.Hmirror).HasColumnName("hmirror");
            entity.Property(e => e.Vflip).HasColumnName("vflip");
            entity.Property(e => e.Aec2).HasColumnName("aec2");
            entity.Property(e => e.Gainceiling).HasColumnName("gainceiling");
            entity.Property(e => e.Dcw).HasColumnName("dcw");
            entity.Property(e => e.Bpc).HasColumnName("bpc");
            entity.Property(e => e.Wpc).HasColumnName("wpc");
            entity.Property(e => e.RawGma).HasColumnName("raw_gma");
            entity.Property(e => e.Lenc).HasColumnName("lenc");
            entity.Property(e => e.CfgHash).HasColumnName("cfg_hash");

            entity.HasIndex(e => e.GaugeId, "ix_camera_config_gauge_id");
            entity.HasIndex(e => e.CfgHash, "ix_camera_config_cfg_hash");
        });
    }
}
