using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OdectyStat1.Migrations.Diagnostics
{
    /// <inheritdoc />
    public partial class AddDoorTravelDiagToGarage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "last_lead_ms",
                table: "garage_diagnostics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "last_travel_ms",
                table: "garage_diagnostics",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_lead_ms",
                table: "garage_diagnostics");

            migrationBuilder.DropColumn(
                name: "last_travel_ms",
                table: "garage_diagnostics");
        }
    }
}
