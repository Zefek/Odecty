using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OdectyStat1.Migrations.Gauge
{
    /// <inheritdoc />
    public partial class AddBillingPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EnergyFactor",
                table: "Gauge",
                type: "numeric(19,12)",
                precision: 19,
                scale: 12,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.Sql("""
                UPDATE "Gauge"
                SET "EnergyFactor" = "PriceUnitFactor" / 0.001,
                    "PriceUnitFactor" = 0.001
                WHERE "PriceUnitFactor" NOT IN (0.001, 1);
                """);

            migrationBuilder.CreateTable(
                name: "BillingPeriod",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GaugeId = table.Column<int>(type: "integer", nullable: false),
                    PeriodFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodTo = table.Column<DateOnly>(type: "date", nullable: false),
                    StartValue = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    EndValue = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    VolumeCoefficient = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    CalorificValue = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPeriod", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingPeriod_Gauge_GaugeId",
                        column: x => x.GaugeId,
                        principalTable: "Gauge",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPeriod_GaugeId_PeriodFrom",
                table: "BillingPeriod",
                columns: new[] { "GaugeId", "PeriodFrom" },
                unique: true);

            migrationBuilder.Sql("""
                DROP VIEW "GaugeMonthlyCost";
                DROP VIEW "GaugeDailyCost";

                CREATE VIEW "GaugeDailyCost" AS
                SELECT d."GaugeId",
                       d."MeasurementDateTime",
                       d."Value",
                       d."CurrentValue",
                       d."EnergyFactor",
                       d."Value" * d."EnergyFactor" AS "Energy",
                       d."Value" * d."EnergyFactor" * d."PriceUnitFactor" AS "PricedQuantity",
                       d."UnitPrice",
                       d."MonthlyFee",
                       d."Value" * d."EnergyFactor" * d."PriceUnitFactor" * d."UnitPrice" AS "ConsumptionCost",
                       d."MonthlyFee" / d."DaysInMonth" AS "FeeCost",
                       d."Value" * d."EnergyFactor" * d."PriceUnitFactor" * d."UnitPrice" + d."MonthlyFee" / d."DaysInMonth" AS "Cost"
                FROM (
                    SELECT s."GaugeId",
                           s."MeasurementDateTime",
                           s."Value",
                           s."CurrentValue",
                           COALESCE(bp."VolumeCoefficient" * bp."CalorificValue", g."EnergyFactor") AS "EnergyFactor",
                           g."PriceUnitFactor",
                           t."UnitPrice",
                           t."MonthlyFee",
                           EXTRACT(DAY FROM date_trunc('month', s."MeasurementDateTime") + INTERVAL '1 month' - INTERVAL '1 day') AS "DaysInMonth"
                    FROM "GaugeMeasurementStatistics" s
                    JOIN "Gauge" g ON g."Id" = s."GaugeId"
                    LEFT JOIN LATERAL (
                        SELECT b."VolumeCoefficient", b."CalorificValue"
                        FROM "BillingPeriod" b
                        WHERE b."GaugeId" = s."GaugeId" AND b."PeriodFrom" <= s."MeasurementDateTime"::date
                        ORDER BY b."PeriodFrom" DESC
                        LIMIT 1
                    ) bp ON true
                    LEFT JOIN LATERAL (
                        SELECT tr."UnitPrice", tr."MonthlyFee"
                        FROM "Tariff" tr
                        WHERE tr."GaugeId" = s."GaugeId" AND tr."ValidFrom" <= s."MeasurementDateTime"::date
                        ORDER BY tr."ValidFrom" DESC
                        LIMIT 1
                    ) t ON true
                ) d;

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP VIEW "GaugeMonthlyCost";
                DROP VIEW "GaugeDailyCost";

                UPDATE "Gauge"
                SET "PriceUnitFactor" = "PriceUnitFactor" * "EnergyFactor"
                WHERE "EnergyFactor" <> 1;
                """);

            migrationBuilder.DropTable(
                name: "BillingPeriod");

            migrationBuilder.DropColumn(
                name: "EnergyFactor",
                table: "Gauge");

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
    }
}
