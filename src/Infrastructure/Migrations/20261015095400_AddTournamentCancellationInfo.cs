using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentCancellationInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tournaments_Name",
                table: "Tournaments");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Tournaments",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Tournaments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                table: "Tournaments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "Tournaments");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_Name",
                table: "Tournaments",
                column: "Name",
                unique: true);
        }
    }
}
