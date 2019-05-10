using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace viafront3.Data.Migrations
{
    public partial class AuthenticationTickets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthenticationTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    Value = table.Column<byte[]>(nullable: true),
                    LastActivity = table.Column<DateTimeOffset>(nullable: true),
                    Expires = table.Column<DateTimeOffset>(nullable: true),
                    RemoteIpAddress = table.Column<string>(nullable: true),
                    OperatingSystem = table.Column<string>(nullable: true),
                    UserAgentFamily = table.Column<string>(nullable: true),
                    UserAgentVersion = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthenticationTickets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationTickets_UserId",
                table: "AuthenticationTickets",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthenticationTickets");
        }
    }
}
