using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeuCrudCsharp.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlanAndSubscriptionModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Frequency",
                table: "Plans",
                newName: "FrequencyInterval");

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPeriodEndDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPeriodStartDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "FrequencyType",
                table: "Plans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "LastFourDigits",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CurrentPeriodEndDate",
                table: "Subscriptions",
                column: "CurrentPeriodEndDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CurrentPeriodEndDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodEndDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodStartDate",
                table: "Subscriptions");

            migrationBuilder.RenameColumn(
                name: "FrequencyInterval",
                table: "Plans",
                newName: "Frequency");

            migrationBuilder.AlterColumn<string>(
                name: "FrequencyType",
                table: "Plans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "LastFourDigits",
                table: "Payments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
