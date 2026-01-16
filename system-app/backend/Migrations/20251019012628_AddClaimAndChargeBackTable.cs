using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeuCrudCsharp.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimAndChargeBackTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MercadoPagoCustomerId",
                table: "AspNetUsers",
                newName: "CustomerId");

            migrationBuilder.CreateTable(
                name: "Chargebacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargebackId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InternalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chargebacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chargebacks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InternalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MercadoPagoClaimUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TypePayment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chargebacks_ChargebackId",
                table: "Chargebacks",
                column: "ChargebackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chargebacks_PaymentId",
                table: "Chargebacks",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Chargebacks_UserId",
                table: "Chargebacks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chargebacks");

            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "AspNetUsers",
                newName: "MercadoPagoCustomerId");
        }
    }
}
