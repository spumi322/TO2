using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedGameColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "TeamAScore",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "TeamBScore",
                table: "Games");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "Games",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamAScore",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamBScore",
                table: "Games",
                type: "integer",
                nullable: true);
        }
    }
}
