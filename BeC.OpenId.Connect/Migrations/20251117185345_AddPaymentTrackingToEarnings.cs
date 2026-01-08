using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeC.OpenId.Connect.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTrackingToEarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EarnedAt",
                table: "Earnings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Earnings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                table: "Earnings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayoutId",
                table: "Earnings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FavoriteDrivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteDrivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteDrivers_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteDrivers_DriverId",
                table: "FavoriteDrivers",
                column: "DriverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteDrivers");

            migrationBuilder.DropColumn(
                name: "EarnedAt",
                table: "Earnings");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Earnings");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Earnings");

            migrationBuilder.DropColumn(
                name: "PayoutId",
                table: "Earnings");
        }
    }
}
