using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OdectyStat1.Migrations.Gauge
{
    /// <inheritdoc />
    public partial class AddBillingDaily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP VIEW "GaugeBillingForecast";

                CREATE VIEW "GaugeBillingDaily" AS
                WITH cur AS (
                    SELECT DISTINCT ON (b."GaugeId")
                           b."GaugeId",
                           b."PeriodTo" + 1 AS "PeriodFrom",
                           b."PeriodTo" + (b."PeriodTo" - b."PeriodFrom" + 1) AS "PeriodTo",
                           b."EndValue" AS "StartValue",
                           b."EndValue" - b."StartValue" AS "PreviousPeriodValue",
                           (b."EndValue" - b."StartValue") / (b."PeriodTo" - b."PeriodFrom" + 1) AS "PreviousDailyValue",
                           b."VolumeCoefficient" * b."CalorificValue" AS "PeriodEnergyFactor"
                    FROM "BillingPeriod" b
                    ORDER BY b."GaugeId", b."PeriodFrom" DESC
                ),
                stat AS (
                    SELECT "GaugeId", "MeasurementDateTime"::date AS "Day", "Value"
                    FROM "GaugeMeasurementStatistics"
                ),
                lastday AS (
                    SELECT "GaugeId", max("Day") AS "LastDay"
                    FROM stat
                    GROUP BY "GaugeId"
                ),
                recent AS (
                    SELECT s."GaugeId", avg(s."Value") AS "RecentDailyValue"
                    FROM stat s
                    JOIN lastday l ON l."GaugeId" = s."GaugeId"
                    WHERE s."Day" > l."LastDay" - 30
                    GROUP BY s."GaugeId"
                ),
                daily AS (
                    SELECT c."GaugeId",
                           c."PeriodFrom",
                           c."PeriodTo",
                           c."StartValue",
                           c."PreviousPeriodValue",
                           c."PreviousDailyValue",
                           l."LastDay",
                           gs.d::date AS "Day",
                           gs.d::date - c."PeriodFrom" + 1 AS "DayIndex",
                           a."Value" IS NOT NULL AS "IsActual",
                           COALESCE(a."Value", f."HistoryValue", r."RecentDailyValue") AS "Value",
                           COALESCE(
                               a."Cost",
                               COALESCE(f."HistoryValue", r."RecentDailyValue") * COALESCE(c."PeriodEnergyFactor", g."EnergyFactor") * g."PriceUnitFactor" * t."UnitPrice"
                                   + t."MonthlyFee" / EXTRACT(DAY FROM date_trunc('month', gs.d) + INTERVAL '1 month' - INTERVAL '1 day')
                           ) AS "Cost",
                           COALESCE(ad."MonthlyAmount", 0) / EXTRACT(DAY FROM date_trunc('month', gs.d) + INTERVAL '1 month' - INTERVAL '1 day') AS "AdvanceShare"
                    FROM cur c
                    JOIN "Gauge" g ON g."Id" = c."GaugeId"
                    CROSS JOIN LATERAL generate_series(c."PeriodFrom"::timestamp, c."PeriodTo"::timestamp, INTERVAL '1 day') AS gs(d)
                    LEFT JOIN lastday l ON l."GaugeId" = c."GaugeId"
                    LEFT JOIN recent r ON r."GaugeId" = c."GaugeId"
                    LEFT JOIN "GaugeDailyCost" a
                           ON a."GaugeId" = c."GaugeId"
                          AND a."MeasurementDateTime"::date = gs.d::date
                    LEFT JOIN LATERAL (
                        SELECT avg(h."Value") AS "HistoryValue"
                        FROM stat h
                        WHERE h."GaugeId" = c."GaugeId"
                          AND h."Day" IN ((gs.d - INTERVAL '1 year')::date, (gs.d - INTERVAL '2 years')::date, (gs.d - INTERVAL '3 years')::date)
                    ) f ON true
                    LEFT JOIN LATERAL (
                        SELECT tr."UnitPrice", tr."MonthlyFee"
                        FROM "Tariff" tr
                        WHERE tr."GaugeId" = c."GaugeId" AND tr."ValidFrom" <= gs.d::date
                        ORDER BY tr."ValidFrom" DESC
                        LIMIT 1
                    ) t ON true
                    LEFT JOIN LATERAL (
                        SELECT x."MonthlyAmount"
                        FROM "Advance" x
                        WHERE x."GaugeId" = c."GaugeId" AND x."ValidFrom" <= gs.d::date
                        ORDER BY x."ValidFrom" DESC
                        LIMIT 1
                    ) ad ON true
                )
                SELECT d."GaugeId",
                       d."PeriodFrom",
                       d."PeriodTo",
                       d."StartValue",
                       d."PreviousPeriodValue",
                       d."PreviousDailyValue",
                       d."LastDay",
                       d."Day",
                       d."DayIndex",
                       d."IsActual",
                       d."Value",
                       d."Cost",
                       d."AdvanceShare",
                       d."PreviousDailyValue" * d."DayIndex" AS "ExpectedCumulative",
                       CASE WHEN d."IsActual" THEN sum(d."Value") OVER w END AS "ActualCumulative",
                       sum(d."Value") OVER w AS "ForecastCumulative",
                       CASE WHEN d."IsActual" THEN sum(d."Cost") OVER w END AS "ActualCostCumulative",
                       sum(d."Cost") OVER w AS "ForecastCostCumulative",
                       sum(d."AdvanceShare") OVER w AS "AdvancesCumulative"
                FROM daily d
                WINDOW w AS (PARTITION BY d."GaugeId" ORDER BY d."Day");

                CREATE VIEW "GaugeBillingForecast" AS
                SELECT d."GaugeId",
                       d."PeriodFrom",
                       d."PeriodTo",
                       d."LastDay",
                       count(*) AS "DaysTotal",
                       count(*) FILTER (WHERE d."IsActual") AS "DaysActual",
                       d."StartValue",
                       g."LastValue" - d."StartValue" AS "MeteredValue",
                       d."PreviousPeriodValue",
                       sum(d."Value") FILTER (WHERE d."IsActual") AS "ActualValue",
                       sum(d."Cost") FILTER (WHERE d."IsActual") AS "ActualCost",
                       sum(d."Value") FILTER (WHERE NOT d."IsActual") AS "RemainingValue",
                       sum(d."Cost") FILTER (WHERE NOT d."IsActual") AS "RemainingCost",
                       sum(d."Value") AS "TotalValue",
                       sum(d."Cost") AS "TotalCost",
                       sum(d."AdvanceShare") FILTER (WHERE d."Day" <= d."LastDay") AS "AdvancesToDate",
                       sum(d."AdvanceShare") AS "AdvancesTotal",
                       sum(d."Cost") - sum(d."AdvanceShare") AS "ExpectedBalance"
                FROM "GaugeBillingDaily" d
                JOIN "Gauge" g ON g."Id" = d."GaugeId"
                GROUP BY d."GaugeId", d."PeriodFrom", d."PeriodTo", d."LastDay", d."StartValue", d."PreviousPeriodValue", g."LastValue";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP VIEW "GaugeBillingForecast";
                DROP VIEW "GaugeBillingDaily";

                CREATE VIEW "GaugeBillingForecast" AS
                WITH cur AS (
                    SELECT DISTINCT ON (b."GaugeId")
                           b."GaugeId",
                           b."PeriodTo" + 1 AS "PeriodFrom",
                           b."PeriodTo" + (b."PeriodTo" - b."PeriodFrom" + 1) AS "PeriodTo",
                           b."EndValue" AS "StartValue",
                           b."EndValue" - b."StartValue" AS "PreviousPeriodValue",
                           b."VolumeCoefficient" * b."CalorificValue" AS "PeriodEnergyFactor"
                    FROM "BillingPeriod" b
                    ORDER BY b."GaugeId", b."PeriodFrom" DESC
                ),
                stat AS (
                    SELECT "GaugeId", "MeasurementDateTime"::date AS "Day", "Value"
                    FROM "GaugeMeasurementStatistics"
                ),
                lastday AS (
                    SELECT "GaugeId", max("Day") AS "LastDay"
                    FROM stat
                    GROUP BY "GaugeId"
                ),
                recent AS (
                    SELECT s."GaugeId", avg(s."Value") AS "RecentDailyValue"
                    FROM stat s
                    JOIN lastday l ON l."GaugeId" = s."GaugeId"
                    WHERE s."Day" > l."LastDay" - 30
                    GROUP BY s."GaugeId"
                ),
                daily AS (
                    SELECT c."GaugeId",
                           gs.d::date AS "Day",
                           a."Value" IS NOT NULL AS "IsActual",
                           COALESCE(a."Value", f."HistoryValue", r."RecentDailyValue") AS "Value",
                           COALESCE(
                               a."Cost",
                               COALESCE(f."HistoryValue", r."RecentDailyValue") * COALESCE(c."PeriodEnergyFactor", g."EnergyFactor") * g."PriceUnitFactor" * t."UnitPrice"
                                   + t."MonthlyFee" / EXTRACT(DAY FROM date_trunc('month', gs.d) + INTERVAL '1 month' - INTERVAL '1 day')
                           ) AS "Cost",
                           COALESCE(ad."MonthlyAmount", 0) / EXTRACT(DAY FROM date_trunc('month', gs.d) + INTERVAL '1 month' - INTERVAL '1 day') AS "AdvanceShare"
                    FROM cur c
                    JOIN "Gauge" g ON g."Id" = c."GaugeId"
                    CROSS JOIN LATERAL generate_series(c."PeriodFrom"::timestamp, c."PeriodTo"::timestamp, INTERVAL '1 day') AS gs(d)
                    LEFT JOIN recent r ON r."GaugeId" = c."GaugeId"
                    LEFT JOIN "GaugeDailyCost" a
                           ON a."GaugeId" = c."GaugeId"
                          AND a."MeasurementDateTime"::date = gs.d::date
                    LEFT JOIN LATERAL (
                        SELECT avg(h."Value") AS "HistoryValue"
                        FROM stat h
                        WHERE h."GaugeId" = c."GaugeId"
                          AND h."Day" IN ((gs.d - INTERVAL '1 year')::date, (gs.d - INTERVAL '2 years')::date, (gs.d - INTERVAL '3 years')::date)
                    ) f ON true
                    LEFT JOIN LATERAL (
                        SELECT tr."UnitPrice", tr."MonthlyFee"
                        FROM "Tariff" tr
                        WHERE tr."GaugeId" = c."GaugeId" AND tr."ValidFrom" <= gs.d::date
                        ORDER BY tr."ValidFrom" DESC
                        LIMIT 1
                    ) t ON true
                    LEFT JOIN LATERAL (
                        SELECT x."MonthlyAmount"
                        FROM "Advance" x
                        WHERE x."GaugeId" = c."GaugeId" AND x."ValidFrom" <= gs.d::date
                        ORDER BY x."ValidFrom" DESC
                        LIMIT 1
                    ) ad ON true
                )
                SELECT c."GaugeId",
                       c."PeriodFrom",
                       c."PeriodTo",
                       l."LastDay",
                       count(*) AS "DaysTotal",
                       count(*) FILTER (WHERE d."IsActual") AS "DaysActual",
                       c."StartValue",
                       g."LastValue" - c."StartValue" AS "MeteredValue",
                       c."PreviousPeriodValue",
                       sum(d."Value") FILTER (WHERE d."IsActual") AS "ActualValue",
                       sum(d."Cost") FILTER (WHERE d."IsActual") AS "ActualCost",
                       sum(d."Value") FILTER (WHERE NOT d."IsActual") AS "RemainingValue",
                       sum(d."Cost") FILTER (WHERE NOT d."IsActual") AS "RemainingCost",
                       sum(d."Value") AS "TotalValue",
                       sum(d."Cost") AS "TotalCost",
                       sum(d."AdvanceShare") FILTER (WHERE d."Day" <= l."LastDay") AS "AdvancesToDate",
                       sum(d."AdvanceShare") AS "AdvancesTotal",
                       sum(d."Cost") - sum(d."AdvanceShare") AS "ExpectedBalance"
                FROM daily d
                JOIN cur c ON c."GaugeId" = d."GaugeId"
                JOIN "Gauge" g ON g."Id" = d."GaugeId"
                LEFT JOIN lastday l ON l."GaugeId" = d."GaugeId"
                GROUP BY c."GaugeId", c."PeriodFrom", c."PeriodTo", c."StartValue", c."PreviousPeriodValue", g."LastValue", l."LastDay";
                """);
        }
    }
}
