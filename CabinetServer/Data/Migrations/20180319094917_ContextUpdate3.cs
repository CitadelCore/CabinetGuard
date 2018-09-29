using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace CabinetServer.Data.Migrations
{
    public partial class ContextUpdate3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OperatorUpn",
                table: "ScheduledCommands",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "OwnerUpn",
                table: "Controllers",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Nickname",
                table: "Cabinets",
                newName: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ScheduledCommands",
                newName: "OperatorUpn");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Controllers",
                newName: "OwnerUpn");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Cabinets",
                newName: "Nickname");
        }
    }
}
