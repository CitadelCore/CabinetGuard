using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace CabinetServer.Web.Data.Migrations
{
    public partial class ContextUpdate4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Nickname",
                table: "Controllers",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Authorized",
                table: "Controllers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Hostname",
                table: "Controllers",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Controllers_Nickname_Hostname",
                table: "Controllers",
                columns: new[] { "Nickname", "Hostname" },
                unique: true,
                filter: "[Nickname] IS NOT NULL AND [Hostname] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Controllers_Nickname_Hostname",
                table: "Controllers");

            migrationBuilder.DropColumn(
                name: "Authorized",
                table: "Controllers");

            migrationBuilder.DropColumn(
                name: "Hostname",
                table: "Controllers");

            migrationBuilder.AlterColumn<string>(
                name: "Nickname",
                table: "Controllers",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
