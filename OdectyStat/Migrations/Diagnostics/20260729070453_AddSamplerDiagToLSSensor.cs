using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OdectyStat1.Migrations.Diagnostics
{
    /// <inheritdoc />
    public partial class AddSamplerDiagToLSSensor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sampler_max_us",
                table: "ls_sensor_diagnostics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sampler_overruns",
                table: "ls_sensor_diagnostics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sampler_stack_words",
                table: "ls_sensor_diagnostics",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sampler_max_us",
                table: "ls_sensor_diagnostics");

            migrationBuilder.DropColumn(
                name: "sampler_overruns",
                table: "ls_sensor_diagnostics");

            migrationBuilder.DropColumn(
                name: "sampler_stack_words",
                table: "ls_sensor_diagnostics");
        }
    }
}
