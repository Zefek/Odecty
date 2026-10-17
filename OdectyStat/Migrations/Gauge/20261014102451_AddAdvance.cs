using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OdectyStat1.Migrations.Gauge
{
    /// <inheritdoc />
    public partial class AddAdvance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Advance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GaugeId = table.Column<int>(type: "integer", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Advance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Advance_Gauge_GaugeId",
                        column: x => x.GaugeId,
                        principalTable: "Gauge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Advance_GaugeId_ValidFrom",
                table: "Advance",
                columns: new[] { "GaugeId", "ValidFrom" },
                unique: true);

            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW "GaugeMonthlyCost" AS
                SELECT m."GaugeId",
                       m."Month",
                       m."Days",
                       m."Value",
                       m."Energy",
                       m."PricedQuantity",
                       m."ConsumptionCost",
                       m."FeeCost",
                       m."Cost",
                       COALESCE(a."MonthlyAmount", 0) AS "Advance",
                       m."Cost" - COALESCE(a."MonthlyAmount", 0) AS "Balance"
                FROM (
                    SELECT "GaugeId",
                           date_trunc('month', "MeasurementDateTime")::date AS "Month",
                           count(*) AS "Days",
                           sum("Value") AS "Value",
                           sum("Energy") AS "Energy",
                           sum("PricedQuantity") AS "PricedQuantity",
                           sum("ConsumptionCost") AS "ConsumptionCost",
                           sum("FeeCost") AS "FeeCost",
                           sum("Cost") AS "Cost"
                    FROM "GaugeDailyCost"
                    GROUP BY "GaugeId", date_trunc('month', "MeasurementDateTime")::date
                ) m
                LEFT JOIN LATERAL (
                    SELECT ad."MonthlyAmount"
                    FROM "Advance" ad
                    WHERE ad."GaugeId" = m."GaugeId" AND ad."ValidFrom" <= m."Month"
                    ORDER BY ad."ValidFrom" DESC
                    LIMIT 1
                ) a ON true;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP VIEW "GaugeMonthlyCost";

                CREATE VIEW "GaugeMonthlyCost" AS
                SELECT "GaugeId",
                       date_trunc('month', "MeasurementDateTime")::date AS "Month",
                       count(*) AS "Days",
                       sum("Value") AS "Value",
                       sum("Energy") AS "Energy",
                       sum("PricedQuantity") AS "PricedQuantity",
                       sum("ConsumptionCost") AS "ConsumptionCost",
                       sum("FeeCost") AS "FeeCost",
                       sum("Cost") AS "Cost"
                FROM "GaugeDailyCost"
                GROUP BY "GaugeId", date_trunc('month', "MeasurementDateTime")::date;
                """);

            migrationBuilder.DropTable(
                name: "Advance");
        }
    }
}
