using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReworkTeamsTournamentsTableToTournamentParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamsTournaments");

            migrationBuilder.CreateTable(
                name: "TournamentParticipants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<long>(type: "INTEGER", nullable: false),
                    TournamentId = table.Column<long>(type: "INTEGER", nullable: false),
                    StandingId = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Eliminated = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_Standings_StandingId",
                        column: x => x.StandingId,
                        principalTable: "Standings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_StandingId",
                table: "TournamentParticipants",
                column: "StandingId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TeamId",
                table: "TournamentParticipants",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId",
                table: "TournamentParticipants",
                column: "TournamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentParticipants");

            migrationBuilder.CreateTable(
                name: "TeamsTournaments",
                columns: table => new
                {
                    TeamId = table.Column<long>(type: "INTEGER", nullable: false),
                    TournamentId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamsTournaments", x => new { x.TeamId, x.TournamentId });
                    table.ForeignKey(
                        name: "FK_TeamsTournaments_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamsTournaments_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamsTournaments_TournamentId",
                table: "TeamsTournaments",
                column: "TournamentId");
        }
    }
}
