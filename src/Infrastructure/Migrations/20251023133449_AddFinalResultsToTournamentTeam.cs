using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalResultsToTournamentTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EliminatedInRound",
                table: "TournamentTeams",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalPlacement",
                table: "TournamentTeams",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultFinalizedAt",
                table: "TournamentTeams",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EliminatedInRound",
                table: "TournamentTeams");

            migrationBuilder.DropColumn(
                name: "FinalPlacement",
                table: "TournamentTeams");

            migrationBuilder.DropColumn(
                name: "ResultFinalizedAt",
                table: "TournamentTeams");
        }
    }
}
