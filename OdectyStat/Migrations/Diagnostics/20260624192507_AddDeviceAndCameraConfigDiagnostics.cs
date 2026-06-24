using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OdectyStat1.Migrations.Diagnostics
{
    /// <inheritdoc />
    public partial class AddDeviceAndCameraConfigDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "camera_config",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gauge_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    schema_ver = table.Column<byte>(type: "smallint", nullable: false),
                    fw_version = table.Column<int>(type: "integer", nullable: false),
                    framesize = table.Column<byte>(type: "smallint", nullable: false),
                    quality = table.Column<byte>(type: "smallint", nullable: false),
                    aec_value = table.Column<int>(type: "integer", nullable: false),
                    exposure_ctrl = table.Column<byte>(type: "smallint", nullable: false),
                    gain_ctrl = table.Column<byte>(type: "smallint", nullable: false),
                    agc_gain = table.Column<byte>(type: "smallint", nullable: false),
                    ae_level = table.Column<short>(type: "smallint", nullable: false),
                    brightness = table.Column<short>(type: "smallint", nullable: false),
                    contrast = table.Column<short>(type: "smallint", nullable: false),
                    saturation = table.Column<short>(type: "smallint", nullable: false),
                    whitebal = table.Column<byte>(type: "smallint", nullable: false),
                    awb_gain = table.Column<byte>(type: "smallint", nullable: false),
                    wb_mode = table.Column<byte>(type: "smallint", nullable: false),
                    special_effect = table.Column<byte>(type: "smallint", nullable: false),
                    hmirror = table.Column<byte>(type: "smallint", nullable: false),
                    vflip = table.Column<byte>(type: "smallint", nullable: false),
                    aec2 = table.Column<byte>(type: "smallint", nullable: false),
                    gainceiling = table.Column<byte>(type: "smallint", nullable: false),
                    dcw = table.Column<byte>(type: "smallint", nullable: false),
                    bpc = table.Column<byte>(type: "smallint", nullable: false),
                    wpc = table.Column<byte>(type: "smallint", nullable: false),
                    raw_gma = table.Column<byte>(type: "smallint", nullable: false),
                    lenc = table.Column<byte>(type: "smallint", nullable: false),
                    cfg_hash = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_camera_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "device_diagnostics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gauge_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    schema_ver = table.Column<byte>(type: "smallint", nullable: false),
                    uptime_seconds = table.Column<long>(type: "bigint", nullable: false),
                    fw_version = table.Column<int>(type: "integer", nullable: false),
                    free_heap_kb = table.Column<int>(type: "integer", nullable: false),
                    min_free_heap_kb = table.Column<int>(type: "integer", nullable: false),
                    max_alloc_kb = table.Column<int>(type: "integer", nullable: false),
                    wifi_reconnects = table.Column<int>(type: "integer", nullable: false),
                    capture_count = table.Column<int>(type: "integer", nullable: false),
                    send_failures = table.Column<int>(type: "integer", nullable: false),
                    tls_errors = table.Column<int>(type: "integer", nullable: false),
                    ota_failures = table.Column<int>(type: "integer", nullable: false),
                    loop_max_ms = table.Column<int>(type: "integer", nullable: false),
                    camera_errors = table.Column<byte>(type: "smallint", nullable: false),
                    reset_reason = table.Column<byte>(type: "smallint", nullable: false),
                    rssi = table.Column<short>(type: "smallint", nullable: false),
                    cfg_hash = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_diagnostics", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_camera_config_cfg_hash",
                table: "camera_config",
                column: "cfg_hash");

            migrationBuilder.CreateIndex(
                name: "ix_camera_config_gauge_id",
                table: "camera_config",
                column: "gauge_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_diagnostics_gauge_id",
                table: "device_diagnostics",
                column: "gauge_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_diagnostics_timestamp",
                table: "device_diagnostics",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "camera_config");

            migrationBuilder.DropTable(
                name: "device_diagnostics");
        }
    }
}
