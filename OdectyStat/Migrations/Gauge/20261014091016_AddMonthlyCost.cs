using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OdectyStat1.Migrations.Gauge
{
    /// <inheritdoc />
    public partial class AddMonthlyCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE VIEW "GaugeMonthlyCost" AS
                SELECT "GaugeId",
                       date_trunc('month', "MeasurementDateTime")::date AS "Month",
                       count(*) AS "Days",
                       sum("Value") AS "Value",
                       sum("PricedQuantity") AS "PricedQuantity",
                       sum("ConsumptionCost") AS "ConsumptionCost",
                       sum("FeeCost") AS "FeeCost",
                       sum("Cost") AS "Cost"
                FROM "GaugeDailyCost"
                GROUP BY "GaugeId", date_trunc('month', "MeasurementDateTime")::date;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP VIEW "GaugeMonthlyCost";""");
        }
    }
}
