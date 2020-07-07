using Microsoft.EntityFrameworkCore.Migrations;

namespace viafront3.Data.Migrations
{
    public partial class EfCore3_BrokerOrderDotUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "BrokerOrders",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrokerOrders_ApplicationUserId",
                table: "BrokerOrders",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BrokerOrders_AspNetUsers_ApplicationUserId",
                table: "BrokerOrders",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BrokerOrders_AspNetUsers_ApplicationUserId",
                table: "BrokerOrders");

            migrationBuilder.DropIndex(
                name: "IX_BrokerOrders_ApplicationUserId",
                table: "BrokerOrders");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "BrokerOrders",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
