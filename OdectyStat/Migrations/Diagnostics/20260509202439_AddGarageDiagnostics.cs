using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OdectyStat1.Migrations.Diagnostics
{
    /// <inheritdoc />
    public partial class AddGarageDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "garage_diagnostics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    uptime_minutes = table.Column<long>(type: "bigint", nullable: false),
                    free_ram = table.Column<int>(type: "integer", nullable: false),
                    wifi_reconnects = table.Column<int>(type: "integer", nullable: false),
                    mqtt_reconnects = table.Column<int>(type: "integer", nullable: false),
                    sensor_errors = table.Column<byte>(type: "smallint", nullable: false),
                    reset_reason = table.Column<byte>(type: "smallint", nullable: false),
                    loop_max_ms = table.Column<int>(type: "integer", nullable: false),
                    door_cycles = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_garage_diagnostics", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_garage_diagnostics_timestamp",
                table: "garage_diagnostics",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "garage_diagnostics");
        }
    }
}
