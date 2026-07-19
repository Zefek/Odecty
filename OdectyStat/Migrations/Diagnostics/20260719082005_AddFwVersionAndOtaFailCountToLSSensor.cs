using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OdectyStat1.Migrations.Diagnostics
{
    /// <inheritdoc />
    public partial class AddFwVersionAndOtaFailCountToLSSensor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "fw_version",
                table: "ls_sensor_diagnostics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ota_fail_count",
                table: "ls_sensor_diagnostics",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fw_version",
                table: "ls_sensor_diagnostics");

            migrationBuilder.DropColumn(
                name: "ota_fail_count",
                table: "ls_sensor_diagnostics");
        }
    }
}
