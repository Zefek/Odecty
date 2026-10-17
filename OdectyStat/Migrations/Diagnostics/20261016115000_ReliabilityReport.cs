using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OdectyStat1.Migrations.Diagnostics
{
    /// <inheritdoc />
    public partial class ReliabilityReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reliability_daily_issue",
                columns: table => new
                {
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    level = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    template = table.Column<string>(type: "text", nullable: false),
                    exception_type = table.Column<string>(type: "text", nullable: true),
                    location = table.Column<string>(type: "text", nullable: true),
                    count = table.Column<int>(type: "integer", nullable: false),
                    first_ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_ts = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hourly = table.Column<int[]>(type: "integer[]", nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reliability_daily_issue", x => new { x.day, x.fingerprint });
                });

            migrationBuilder.CreateTable(
                name: "reliability_daily_metric",
                columns: table => new
                {
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    metric = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reliability_daily_metric", x => new { x.day, x.source, x.metric, x.key });
                });

            migrationBuilder.CreateTable(
                name: "reliability_daily_report",
                columns: table => new
                {
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    facts = table.Column<string>(type: "jsonb", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    comment_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    comment_tokens_in = table.Column<int>(type: "integer", nullable: true),
                    comment_tokens_out = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reliability_daily_report", x => x.day);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reliability_daily_issue_fingerprint",
                table: "reliability_daily_issue",
                columns: new[] { "fingerprint", "day" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reliability_daily_issue");

            migrationBuilder.DropTable(
                name: "reliability_daily_metric");

            migrationBuilder.DropTable(
                name: "reliability_daily_report");
        }
    }
}
