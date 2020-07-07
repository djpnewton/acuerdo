using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace viafront3.Data.Migrations
{
    public partial class RecipientParams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_BrokerOrderCustomRecipientParams_BrokerOrderId",
                table: "BrokerOrderCustomRecipientParams",
                column: "BrokerOrderId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrokerOrderCustomRecipientParams");
        }
    }
}
