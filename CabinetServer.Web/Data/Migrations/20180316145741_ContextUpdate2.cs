using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace CabinetServer.Web.Data.Migrations
{
    public partial class ContextUpdate2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles");

            migrationBuilder.CreateTable(
                name: "Cabinets",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ControllerId = table.Column<Guid>(nullable: false),
                    FireAlerted = table.Column<bool>(nullable: false),
                    Nickname = table.Column<string>(nullable: true),
                    SecurityAlerted = table.Column<bool>(nullable: false),
                    SecurityArmed = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cabinets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Controllers",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Nickname = table.Column<string>(nullable: true),
                    OwnerUpn = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Controllers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FanModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CabinetId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FanModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fans",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Fanspeed = table.Column<int>(nullable: false),
                    InletTemperature = table.Column<int>(nullable: false),
                    ModuleId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonitoredDoors",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Armed = table.Column<bool>(nullable: false),
                    CabinetId = table.Column<Guid>(nullable: false),
                    CanLock = table.Column<bool>(nullable: false),
                    CanMonitor = table.Column<bool>(nullable: false),
                    Opened = table.Column<bool>(nullable: false),
                    Unlocked = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoredDoors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ControllerId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    OperatorUpn = table.Column<string>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    Payload = table.Column<string>(nullable: true),
                    Result = table.Column<string>(nullable: true),
                    State = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledCommands", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Cabinets");

            migrationBuilder.DropTable(
                name: "Controllers");

            migrationBuilder.DropTable(
                name: "FanModules");

            migrationBuilder.DropTable(
                name: "Fans");

            migrationBuilder.DropTable(
                name: "MonitoredDoors");

            migrationBuilder.DropTable(
                name: "ScheduledCommands");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_UserId",
                table: "AspNetUserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName");
        }
    }
}
