using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BessIntelligence.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialEpic1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PortfolioAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChargeWindowStart = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChargeWindowEnd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DischargeWindowStart = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DischargeWindowEnd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChargePrice = table.Column<double>(type: "float", nullable: false),
                    DischargePrice = table.Column<double>(type: "float", nullable: false),
                    PriceSpreadMultiplier = table.Column<double>(type: "float", nullable: false),
                    Avg30dSpreadMultiplier = table.Column<double>(type: "float", nullable: false),
                    ConfidencePct = table.Column<double>(type: "float", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedCaptureEur = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Batteries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SiteName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Chemistry = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PowerRatingKw = table.Column<int>(type: "int", nullable: false),
                    CapacityKwh = table.Column<int>(type: "int", nullable: false),
                    DurationH = table.Column<double>(type: "float", nullable: false),
                    CommissionedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BatteryModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WiththegridNodeId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batteries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EngineConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceMaePct = table.Column<double>(type: "float", nullable: false),
                    WindRmseMs = table.Column<double>(type: "float", nullable: false),
                    CloudMaePct = table.Column<double>(type: "float", nullable: false),
                    SigmaSohPct = table.Column<double>(type: "float", nullable: false),
                    Avg30dSpreadMultiplier = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EngineConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Market = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HourStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PriceEurMwh = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherForecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HourStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AmbientTempC = table.Column<double>(type: "float", nullable: false),
                    HumidityPct = table.Column<double>(type: "float", nullable: false),
                    WindSpeedMs = table.Column<double>(type: "float", nullable: false),
                    SolarIrradianceWm2 = table.Column<double>(type: "float", nullable: false),
                    CloudCoverPct = table.Column<double>(type: "float", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BatteryActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecommendationId = table.Column<int>(type: "int", nullable: false),
                    BatteryId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WindowStart = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WindowEnd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatteryActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatteryActions_AiRecommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "AiRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatteryActions_Batteries_BatteryId",
                        column: x => x.BatteryId,
                        principalTable: "Batteries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BatteryHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatteryId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PowerKw = table.Column<double>(type: "float", nullable: false),
                    SocPct = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatteryHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatteryHistories_Batteries_BatteryId",
                        column: x => x.BatteryId,
                        principalTable: "Batteries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BatteryTelemetries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatteryId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SocPct = table.Column<double>(type: "float", nullable: false),
                    SohPct = table.Column<double>(type: "float", nullable: false),
                    PowerKw = table.Column<double>(type: "float", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemperatureC = table.Column<double>(type: "float", nullable: false),
                    VoltageV = table.Column<double>(type: "float", nullable: false),
                    CurrentA = table.Column<double>(type: "float", nullable: false),
                    NextAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextActionWindow = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FaultCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatteryTelemetries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatteryTelemetries_Batteries_BatteryId",
                        column: x => x.BatteryId,
                        principalTable: "Batteries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Batteries_Code",
                table: "Batteries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatteryActions_BatteryId",
                table: "BatteryActions",
                column: "BatteryId");

            migrationBuilder.CreateIndex(
                name: "IX_BatteryActions_RecommendationId",
                table: "BatteryActions",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_BatteryHistories_BatteryId_Timestamp",
                table: "BatteryHistories",
                columns: new[] { "BatteryId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_BatteryTelemetries_BatteryId",
                table: "BatteryTelemetries",
                column: "BatteryId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_HourStart",
                table: "MarketPrices",
                column: "HourStart");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherForecasts_SiteId_HourStart",
                table: "WeatherForecasts",
                columns: new[] { "SiteId", "HourStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatteryActions");

            migrationBuilder.DropTable(
                name: "BatteryHistories");

            migrationBuilder.DropTable(
                name: "BatteryTelemetries");

            migrationBuilder.DropTable(
                name: "EngineConfigs");

            migrationBuilder.DropTable(
                name: "MarketPrices");

            migrationBuilder.DropTable(
                name: "WeatherForecasts");

            migrationBuilder.DropTable(
                name: "AiRecommendations");

            migrationBuilder.DropTable(
                name: "Batteries");
        }
    }
}
