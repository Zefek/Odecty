using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OdectyStat1.Migrations.Diagnostics
{
    /// <inheritdoc />
    public partial class AddFanDiagToFve : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "fan_mismatch_slots",
                table: "fve_diagnostics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fan_rpm_a",
                table: "fve_diagnostics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fan_rpm_b",
                table: "fve_diagnostics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fan_run_pct_a",
                table: "fve_diagnostics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fan_run_pct_b",
                table: "fve_diagnostics",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fan_mismatch_slots",
                table: "fve_diagnostics");

            migrationBuilder.DropColumn(
                name: "fan_rpm_a",
                table: "fve_diagnostics");

            migrationBuilder.DropColumn(
                name: "fan_rpm_b",
                table: "fve_diagnostics");

            migrationBuilder.DropColumn(
                name: "fan_run_pct_a",
                table: "fve_diagnostics");

            migrationBuilder.DropColumn(
                name: "fan_run_pct_b",
                table: "fve_diagnostics");
        }
    }
}
