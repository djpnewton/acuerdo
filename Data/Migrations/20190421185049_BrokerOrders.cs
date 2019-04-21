using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace viafront3.Data.Migrations
{
    public partial class BrokerOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrokerOrders",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationUserId = table.Column<string>(nullable: true),
                    Date = table.Column<long>(nullable: false),
                    Expiry = table.Column<long>(nullable: false),
                    Token = table.Column<string>(nullable: true),
                    Market = table.Column<string>(nullable: true),
                    AssetSend = table.Column<string>(nullable: true),
                    AmountSend = table.Column<decimal>(nullable: false),
                    AssetReceive = table.Column<string>(nullable: true),
                    AmountReceive = table.Column<decimal>(nullable: false),
                    Fee = table.Column<decimal>(nullable: false),
                    InvoiceId = table.Column<string>(nullable: true),
                    PaymentAddress = table.Column<string>(nullable: true),
                    PaymentUrl = table.Column<string>(nullable: true),
                    TxIdPayment = table.Column<string>(nullable: true),
                    Recipient = table.Column<string>(nullable: true),
                    TxIdRecipient = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerOrders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerOrders_InvoiceId",
                table: "BrokerOrders",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrokerOrders_Token",
                table: "BrokerOrders",
                column: "Token",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrokerOrders");
        }
    }
}
