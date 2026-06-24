using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OdectyStat1.Migrations.Diagnostics
{
    /// <inheritdoc />
    public partial class AddFileAndTransferDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_diagnostics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    correlation_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    gauge_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    recognized_value = table.Column<decimal>(type: "numeric", nullable: true),
                    corrected_value = table.Column<decimal>(type: "numeric", nullable: true),
                    confidence = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_diagnostics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transfer_diagnostics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    correlation_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    gauge_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    schema_ver = table.Column<byte>(type: "smallint", nullable: false),
                    img_size = table.Column<long>(type: "bigint", nullable: false),
                    bytes_sent = table.Column<long>(type: "bigint", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    try_count = table.Column<byte>(type: "smallint", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    http_code = table.Column<short>(type: "smallint", nullable: false),
                    rssi = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_diagnostics", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_diagnostics_correlation_id",
                table: "file_diagnostics",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_diagnostics_timestamp",
                table: "file_diagnostics",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_diagnostics_correlation_id",
                table: "transfer_diagnostics",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_diagnostics_timestamp",
                table: "transfer_diagnostics",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_diagnostics");

            migrationBuilder.DropTable(
                name: "transfer_diagnostics");
        }
    }
}
