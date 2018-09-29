using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CabinetServer.Web.Data.Migrations
{
    public partial class AddOneview : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OneviewHostname",
                table: "Cabinets",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OneviewPassword",
                table: "Cabinets",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OneviewUsername",
                table: "Cabinets",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OneviewHostname",
                table: "Cabinets");

            migrationBuilder.DropColumn(
                name: "OneviewPassword",
                table: "Cabinets");

            migrationBuilder.DropColumn(
                name: "OneviewUsername",
                table: "Cabinets");
        }
    }
}
