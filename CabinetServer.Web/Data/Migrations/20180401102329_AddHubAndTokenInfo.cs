using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CabinetServer.Web.Data.Migrations
{
    public partial class AddHubAndTokenInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HubConnectionId",
                table: "Controllers",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevocationGuid",
                table: "Controllers",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HubConnectionId",
                table: "Controllers");

            migrationBuilder.DropColumn(
                name: "RevocationGuid",
                table: "Controllers");
        }
    }
}
