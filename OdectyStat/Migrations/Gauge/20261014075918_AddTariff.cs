using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OdectyStat1.Migrations.Gauge
{
    /// <inheritdoc />
    public partial class AddTariff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PriceUnitFactor",
                table: "Gauge",
                type: "numeric(19,12)",
                precision: 19,
                scale: 12,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.CreateTable(
                name: "Tariff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GaugeId = table.Column<int>(type: "integer", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    MonthlyFee = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tariff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tariff_Gauge_GaugeId",
                        column: x => x.GaugeId,
                        principalTable: "Gauge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tariff_GaugeId_ValidFrom",
                table: "Tariff",
                columns: new[] { "GaugeId", "ValidFrom" },
                unique: true);

            migrationBuilder.Sql("""
                CREATE VIEW "GaugeDailyCost" AS
                SELECT d."GaugeId",
                       d."MeasurementDateTime",
                       d."Value",
                       d."CurrentValue",
                       d."PricedQuantity",
                       d."UnitPrice",
                       d."MonthlyFee",
                       d."PricedQuantity" * d."UnitPrice" AS "ConsumptionCost",
                       d."MonthlyFee" / d."DaysInMonth" AS "FeeCost",
                       d."PricedQuantity" * d."UnitPrice" + d."MonthlyFee" / d."DaysInMonth" AS "Cost"
                FROM (
                    SELECT s."GaugeId",
                           s."MeasurementDateTime",
                           s."Value",
                           s."CurrentValue",
                           s."Value" * g."PriceUnitFactor" AS "PricedQuantity",
                           t."UnitPrice",
                           t."MonthlyFee",
                           EXTRACT(DAY FROM date_trunc('month', s."MeasurementDateTime") + INTERVAL '1 month' - INTERVAL '1 day') AS "DaysInMonth"
                    FROM "GaugeMeasurementStatistics" s
                    JOIN "Gauge" g ON g."Id" = s."GaugeId"
                    LEFT JOIN LATERAL (
                        SELECT tr."UnitPrice", tr."MonthlyFee"
                        FROM "Tariff" tr
                        WHERE tr."GaugeId" = s."GaugeId" AND tr."ValidFrom" <= s."MeasurementDateTime"::date
                        ORDER BY tr."ValidFrom" DESC
                        LIMIT 1
                    ) t ON true
                ) d;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP VIEW "GaugeDailyCost";""");

            migrationBuilder.DropTable(
                name: "Tariff");

            migrationBuilder.DropColumn(
                name: "PriceUnitFactor",
                table: "Gauge");
        }
    }
}
