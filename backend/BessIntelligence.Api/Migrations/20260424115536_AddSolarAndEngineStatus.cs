using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BessIntelligence.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSolarAndEngineStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "GridConnectionKw",
                table: "Batteries",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "EngineRunStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EngineRunStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SolarInstallations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    CapacityKwp = table.Column<double>(type: "float", nullable: false),
                    PanelCount = table.Column<int>(type: "int", nullable: false),
                    PanelType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TiltDeg = table.Column<double>(type: "float", nullable: false),
                    AzimuthDeg = table.Column<double>(type: "float", nullable: false),
                    CommissionedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolarInstallations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SolarForecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolarInstallationId = table.Column<int>(type: "int", nullable: false),
                    HourStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ForecastProductionKw = table.Column<double>(type: "float", nullable: false),
                    ConfidenceLowKw = table.Column<double>(type: "float", nullable: false),
                    ConfidenceHighKw = table.Column<double>(type: "float", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolarForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolarForecasts_SolarInstallations_SolarInstallationId",
                        column: x => x.SolarInstallationId,
                        principalTable: "SolarInstallations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolarProductions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolarInstallationId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProductionKw = table.Column<double>(type: "float", nullable: false),
                    IrradianceWm2 = table.Column<double>(type: "float", nullable: false),
                    PanelTempC = table.Column<double>(type: "float", nullable: false),
                    CapacityFactorPct = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolarProductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolarProductions_SolarInstallations_SolarInstallationId",
                        column: x => x.SolarInstallationId,
                        principalTable: "SolarInstallations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EngineRunStatuses_Date",
                table: "EngineRunStatuses",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolarForecasts_SolarInstallationId_HourStart",
                table: "SolarForecasts",
                columns: new[] { "SolarInstallationId", "HourStart" });

            migrationBuilder.CreateIndex(
                name: "IX_SolarInstallations_SiteId",
                table: "SolarInstallations",
                column: "SiteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolarProductions_SolarInstallationId_Timestamp",
                table: "SolarProductions",
                columns: new[] { "SolarInstallationId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EngineRunStatuses");

            migrationBuilder.DropTable(
                name: "SolarForecasts");

            migrationBuilder.DropTable(
                name: "SolarProductions");

            migrationBuilder.DropTable(
                name: "SolarInstallations");

            migrationBuilder.DropColumn(
                name: "GridConnectionKw",
                table: "Batteries");
        }
    }
}
