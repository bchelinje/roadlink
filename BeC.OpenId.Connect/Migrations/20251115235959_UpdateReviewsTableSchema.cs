using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeC.OpenId.Connect.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReviewsTableSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys first
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Drivers_DriverId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Jobs_JobId",
                table: "Reviews");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Reviews_DriverId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_JobId",
                table: "Reviews");

            // Drop old columns
            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "PunctualityRating",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ProfessionalismRating",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "CareOfItemsRating",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "CommunicationRating",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "HelpfulVotes",
                table: "Reviews");

            // Rename DriverResponse to Response
            migrationBuilder.RenameColumn(
                name: "DriverResponse",
                table: "Reviews",
                newName: "Response");

            // Change Response column max length
            migrationBuilder.AlterColumn<string>(
                name: "Response",
                table: "Reviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            // Change JobId to nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "JobId",
                table: "Reviews",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            // Change Status column max length
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Reviews",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "active",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            // Add new columns
            migrationBuilder.AddColumn<string>(
                name: "ReviewerId",
                table: "Reviews",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReviewerName",
                table: "Reviews",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReviewerType",
                table: "Reviews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "customer");

            migrationBuilder.AddColumn<string>(
                name: "RevieweeId",
                table: "Reviews",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RevieweeName",
                table: "Reviews",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RevieweeType",
                table: "Reviews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "driver");

            migrationBuilder.AddColumn<string>(
                name: "Photos",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RespondedBy",
                table: "Reviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFlagged",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FlagReason",
                table: "Reviews",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlaggedBy",
                table: "Reviews",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlaggedDate",
                table: "Reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeratorNotes",
                table: "Reviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModeratedBy",
                table: "Reviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedDate",
                table: "Reviews",
                type: "datetime2",
                nullable: true);

            // Recreate foreign key for Jobs (now nullable)
            migrationBuilder.CreateIndex(
                name: "IX_Reviews_JobId",
                table: "Reviews",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Jobs_JobId",
                table: "Reviews",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Jobs_JobId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_JobId",
                table: "Reviews");

            // Drop new columns
            migrationBuilder.DropColumn(name: "ReviewerId", table: "Reviews");
            migrationBuilder.DropColumn(name: "ReviewerName", table: "Reviews");
            migrationBuilder.DropColumn(name: "ReviewerType", table: "Reviews");
            migrationBuilder.DropColumn(name: "RevieweeId", table: "Reviews");
            migrationBuilder.DropColumn(name: "RevieweeName", table: "Reviews");
            migrationBuilder.DropColumn(name: "RevieweeType", table: "Reviews");
            migrationBuilder.DropColumn(name: "Photos", table: "Reviews");
            migrationBuilder.DropColumn(name: "RespondedBy", table: "Reviews");
            migrationBuilder.DropColumn(name: "IsFlagged", table: "Reviews");
            migrationBuilder.DropColumn(name: "FlagReason", table: "Reviews");
            migrationBuilder.DropColumn(name: "FlaggedBy", table: "Reviews");
            migrationBuilder.DropColumn(name: "FlaggedDate", table: "Reviews");
            migrationBuilder.DropColumn(name: "ModeratorNotes", table: "Reviews");
            migrationBuilder.DropColumn(name: "IsHidden", table: "Reviews");
            migrationBuilder.DropColumn(name: "ModeratedBy", table: "Reviews");
            migrationBuilder.DropColumn(name: "ModeratedDate", table: "Reviews");

            // Rename Response back to DriverResponse
            migrationBuilder.RenameColumn(
                name: "Response",
                table: "Reviews",
                newName: "DriverResponse");

            // Restore old columns
            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Reviews",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Reviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DriverId",
                table: "Reviews",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<int>(
                name: "PunctualityRating",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfessionalismRating",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CareOfItemsRating",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommunicationRating",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HelpfulVotes",
                table: "Reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Restore old foreign keys and indexes
            migrationBuilder.CreateIndex(
                name: "IX_Reviews_DriverId",
                table: "Reviews",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_JobId",
                table: "Reviews",
                column: "JobId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Drivers_DriverId",
                table: "Reviews",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Jobs_JobId",
                table: "Reviews",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
