using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OdectyStat1.Migrations.Gauge
{
    /// <inheritdoc />
    public partial class GaugeMeasurement_Confidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Confidence",
                table: "GaugeMeasurement",
                type: "decimal(19,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "GaugeMeasurement");
        }
    }
}
