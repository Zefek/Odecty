using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OdectyStat1.Migrations.Gauge
{
    /// <inheritdoc />
    public partial class InitPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gauge",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    LastValue = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    MaxValuePerHour = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    InitialValue = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    HomeassistantId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gauge", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GaugeMeasurement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GaugeId = table.Column<int>(type: "integer", nullable: false),
                    MeasurementDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrentValue = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    ImagePath = table.Column<string>(type: "text", nullable: true),
                    LastMeasurementDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GaugeMeasurement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GaugeMeasurement_Gauge_GaugeId",
                        column: x => x.GaugeId,
                        principalTable: "Gauge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GaugeMeasurementStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GaugeId = table.Column<int>(type: "integer", nullable: false),
                    MeasurementDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrentValue = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GaugeMeasurementStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GaugeMeasurementStatistics_Gauge_GaugeId",
                        column: x => x.GaugeId,
                        principalTable: "Gauge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GaugeMeasurement_GaugeId",
                table: "GaugeMeasurement",
                column: "GaugeId");

            migrationBuilder.CreateIndex(
                name: "IX_GaugeMeasurementStatistics_GaugeId",
                table: "GaugeMeasurementStatistics",
                column: "GaugeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GaugeMeasurement");

            migrationBuilder.DropTable(
                name: "GaugeMeasurementStatistics");

            migrationBuilder.DropTable(
                name: "Gauge");
        }
    }
}
