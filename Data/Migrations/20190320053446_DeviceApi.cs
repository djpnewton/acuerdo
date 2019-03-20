using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace viafront3.Data.Migrations
{
    public partial class DeviceApi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountCreationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Date = table.Column<long>(nullable: false),
                    Token = table.Column<string>(nullable: true),
                    Secret = table.Column<string>(nullable: true),
                    Completed = table.Column<bool>(nullable: false),
                    RequestedEmail = table.Column<string>(nullable: true),
                    RequestedPassword = table.Column<string>(nullable: true),
                    RequestedDeviceName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountCreationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCreationRequests",
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
                    Name = table.Column<string>(nullable: true),
                    DeviceKey = table.Column<string>(nullable: true),
                    DeviceSecret = table.Column<string>(nullable: true),
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
                name: "IX_AccountCreationRequests_Token",
                table: "AccountCreationRequests",
                column: "Token",
                unique: true);

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountCreationRequests");

            migrationBuilder.DropTable(
                name: "DeviceCreationRequests");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
