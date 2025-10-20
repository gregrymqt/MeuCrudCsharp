using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeuCrudCsharp.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodIdInSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodId",
                table: "Subscriptions",
                type: "nvarchar(max)",
                nullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "FrequencyType",
                table: "Plans",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PaymentMethodId", table: "Subscriptions");

            migrationBuilder.AlterColumn<int>(
                name: "FrequencyType",
                table: "Plans",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)"
            );
        }
    }
}
