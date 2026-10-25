using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OdectyStat1.Migrations.Gauge
{
    /// <inheritdoc />
    public partial class AddBillingSpread : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE VIEW "GaugeBillingPeriodStats" AS
                SELECT b."GaugeId",
                       b."PeriodFrom",
                       b."PeriodTo",
                       b."PeriodTo" - b."PeriodFrom" + 1 AS "Days",
                       b."EndValue" - b."StartValue" AS "Value",
                       (b."EndValue" - b."StartValue") / (b."PeriodTo" - b."PeriodFrom" + 1) AS "DailyValue"
                FROM "BillingPeriod" b;

                CREATE VIEW "GaugeBillingSpread" AS
                SELECT p."GaugeId",
                       count(*) AS "Periods",
                       min(p."DailyValue") AS "MinDailyValue",
                       max(p."DailyValue") AS "MaxDailyValue",
                       sum(p."Value") / sum(p."Days") AS "AvgDailyValue",
                       (array_agg(p."PeriodFrom" ORDER BY p."DailyValue"))[1] AS "MinPeriodFrom",
                       (array_agg(p."PeriodFrom" ORDER BY p."DailyValue" DESC))[1] AS "MaxPeriodFrom"
                FROM "GaugeBillingPeriodStats" p
                GROUP BY p."GaugeId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP VIEW "GaugeBillingSpread";
                DROP VIEW "GaugeBillingPeriodStats";
                """);
        }
    }
}
