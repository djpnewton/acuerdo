using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace viafront3.Data.Migrations
{
    public partial class RecipientParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ApplicationUserId",
                table: "BrokerOrders",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BrokerOrderCustomRecipientParams",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BrokerOrderId = table.Column<int>(nullable: false),
                    Reference = table.Column<string>(nullable: true),
                    Code = table.Column<string>(nullable: true),
                    Particulars = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerOrderCustomRecipientParams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerOrders_ApplicationUserId",
                table: "BrokerOrders",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BrokerOrderCustomRecipientParams_BrokerOrderId",
                table: "BrokerOrderCustomRecipientParams",
                column: "BrokerOrderId",
                unique: true);

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

            migrationBuilder.DropTable(
                name: "BrokerOrderCustomRecipientParams");

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
