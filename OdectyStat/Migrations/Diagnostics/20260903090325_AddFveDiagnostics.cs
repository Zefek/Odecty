using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OdectyStat1.Migrations.Diagnostics
{
    /// <inheritdoc />
    public partial class AddFveDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fve_diagnostics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    uptime_minutes = table.Column<long>(type: "bigint", nullable: false),
                    free_heap_kb = table.Column<int>(type: "integer", nullable: false),
                    min_free_heap_kb = table.Column<int>(type: "integer", nullable: false),
                    wifi_reconnects = table.Column<int>(type: "integer", nullable: false),
                    mqtt_fail_count = table.Column<int>(type: "integer", nullable: false),
                    ota_fail_count = table.Column<int>(type: "integer", nullable: false),
                    loop_max_ms = table.Column<int>(type: "integer", nullable: false),
                    bel_frame_errors = table.Column<int>(type: "integer", nullable: false),
                    load_dropouts = table.Column<int>(type: "integer", nullable: false),
                    raw_a = table.Column<int>(type: "integer", nullable: false),
                    raw_b = table.Column<int>(type: "integer", nullable: false),
                    ripple_a = table.Column<int>(type: "integer", nullable: false),
                    ripple_b = table.Column<int>(type: "integer", nullable: false),
                    reset_reason = table.Column<byte>(type: "smallint", nullable: false),
                    fw_version = table.Column<int>(type: "integer", nullable: false),
                    rssi = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fve_diagnostics", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fve_diagnostics_timestamp",
                table: "fve_diagnostics",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fve_diagnostics");
        }
    }
}
