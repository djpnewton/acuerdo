using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace viafront3.Data.Migrations
{
    public partial class DeviceToApiKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceCreationRequests");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.CreateTable(
                name: "ApiKeyCreationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationUserId = table.Column<string>(nullable: true),
                    Date = table.Column<long>(nullable: false),
                    Token = table.Column<string>(nullable: true),
                    Secret = table.Column<string>(nullable: true),
                    Completed = table.Column<bool>(nullable: false),
                    RequestedDeviceName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeyCreationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationUserId = table.Column<string>(nullable: true),
                    CreationRequestId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Key = table.Column<string>(nullable: true),
                    Secret = table.Column<string>(nullable: true),
                    Nonce = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyCreationRequests_Token",
                table: "ApiKeyCreationRequests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_ApplicationUserId",
                table: "ApiKeys",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_Key",
                table: "ApiKeys",
                column: "Key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeyCreationRequests");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.CreateTable(
                name: "DeviceCreationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationUserId = table.Column<string>(nullable: true),
                    Completed = table.Column<bool>(nullable: false),
                    Date = table.Column<long>(nullable: false),
                    RequestedDeviceName = table.Column<string>(nullable: true),
                    Secret = table.Column<string>(nullable: true),
                    Token = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCreationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationUserId = table.Column<string>(nullable: true),
                    CreationRequestId = table.Column<int>(nullable: false),
                    DeviceKey = table.Column<string>(nullable: true),
                    DeviceSecret = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Nonce = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCreationRequests_Token",
                table: "DeviceCreationRequests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_ApplicationUserId",
                table: "Devices",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceKey",
                table: "Devices",
                column: "DeviceKey",
                unique: true);
        }
    }
}
