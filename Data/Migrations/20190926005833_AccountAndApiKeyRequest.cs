using Microsoft.EntityFrameworkCore.Migrations;

namespace viafront3.Data.Migrations
{
    public partial class AccountAndApiKeyRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreationRequestId",
                table: "ApiKeys",
                newName: "ApiKeyCreationRequestId");

            migrationBuilder.AddColumn<int>(
                name: "AccountCreationRequestId",
                table: "ApiKeys",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountCreationRequestId",
                table: "ApiKeys");

            migrationBuilder.RenameColumn(
                name: "ApiKeyCreationRequestId",
                table: "ApiKeys",
                newName: "CreationRequestId");
        }
    }
}
