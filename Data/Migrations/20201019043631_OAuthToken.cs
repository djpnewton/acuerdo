using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace viafront3.Data.Migrations
{
    public partial class OAuthToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OAuthTokens",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationUserId = table.Column<string>(nullable: true),
                    Date = table.Column<long>(nullable: false),
                    ExpiresIn = table.Column<long>(nullable: false),
                    ExpiresAt = table.Column<long>(nullable: false),
                    AccessToken = table.Column<string>(nullable: true),
                    Scope = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthTokens_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthTokens_AccessToken",
                table: "OAuthTokens",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthTokens_ApplicationUserId",
                table: "OAuthTokens",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BrokerOrderCustomRecipientParams_BrokerOrders_BrokerOrderId",
                table: "BrokerOrderCustomRecipientParams",
                column: "BrokerOrderId",
                principalTable: "BrokerOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BrokerOrderCustomRecipientParams_BrokerOrders_BrokerOrderId",
                table: "BrokerOrderCustomRecipientParams");

            migrationBuilder.DropTable(
                name: "OAuthTokens");
        }
    }
}
