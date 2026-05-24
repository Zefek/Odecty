using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OdectyStat1.Migrations.Diagnostics
{
    /// <inheritdoc />
    public partial class AddRssiToDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "rssi",
                table: "ls_sensor_diagnostics",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "rssi",
                table: "heater_diagnostics",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "rssi",
                table: "garage_diagnostics",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rssi",
                table: "ls_sensor_diagnostics");

            migrationBuilder.DropColumn(
                name: "rssi",
                table: "heater_diagnostics");

            migrationBuilder.DropColumn(
                name: "rssi",
                table: "garage_diagnostics");
        }
    }
}
