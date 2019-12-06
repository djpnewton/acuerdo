using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace viafront3.Data.Migrations
{
    public partial class BrokerOrderWithdrawal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrokerOrderChainWithdrawals",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BrokerOrderId = table.Column<int>(nullable: false),
                    SpendCode = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerOrderChainWithdrawals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BrokerOrderFiatWithdrawals",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BrokerOrderId = table.Column<int>(nullable: false),
                    DepositCode = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerOrderFiatWithdrawals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerOrderChainWithdrawals_BrokerOrderId",
                table: "BrokerOrderChainWithdrawals",
                column: "BrokerOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrokerOrderChainWithdrawals_SpendCode",
                table: "BrokerOrderChainWithdrawals",
                column: "SpendCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrokerOrderFiatWithdrawals_BrokerOrderId",
                table: "BrokerOrderFiatWithdrawals",
                column: "BrokerOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrokerOrderFiatWithdrawals_DepositCode",
                table: "BrokerOrderFiatWithdrawals",
                column: "DepositCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrokerOrderChainWithdrawals");

            migrationBuilder.DropTable(
                name: "BrokerOrderFiatWithdrawals");
        }
    }
}
