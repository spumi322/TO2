using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReaddTeamsTournaments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamTournament");

            migrationBuilder.RenameColumn(
                name: "TournamentsId",
                table: "TeamsTournaments",
                newName: "TournamentId");

            migrationBuilder.RenameColumn(
                name: "TeamsId",
                table: "TeamsTournaments",
                newName: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamsTournaments_TournamentId",
                table: "TeamsTournaments",
                column: "TournamentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamsTournaments_Teams_TeamId",
                table: "TeamsTournaments",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamsTournaments_Tournaments_TournamentId",
                table: "TeamsTournaments",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamsTournaments_Teams_TeamId",
                table: "TeamsTournaments");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamsTournaments_Tournaments_TournamentId",
                table: "TeamsTournaments");

            migrationBuilder.DropIndex(
                name: "IX_TeamsTournaments_TournamentId",
                table: "TeamsTournaments");

            migrationBuilder.RenameColumn(
                name: "TournamentId",
                table: "TeamsTournaments",
                newName: "TournamentsId");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "TeamsTournaments",
                newName: "TeamsId");

            migrationBuilder.CreateTable(
                name: "TeamTournament",
                columns: table => new
                {
                    TeamsId = table.Column<long>(type: "INTEGER", nullable: false),
                    TournamentsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTournament", x => new { x.TeamsId, x.TournamentsId });
                    table.ForeignKey(
                        name: "FK_TeamTournament_Teams_TeamsId",
                        column: x => x.TeamsId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamTournament_Tournaments_TournamentsId",
                        column: x => x.TournamentsId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamTournament_TournamentsId",
                table: "TeamTournament",
                column: "TournamentsId");
        }
    }
}
