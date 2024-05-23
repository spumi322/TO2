using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TeamTournamentCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Match_MatchId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Match_Standings_StandingId",
                table: "Match");

            migrationBuilder.DropForeignKey(
                name: "FK_Match_Teams_TeamAId",
                table: "Match");

            migrationBuilder.DropForeignKey(
                name: "FK_Match_Teams_TeamBId",
                table: "Match");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Match",
                table: "Match");

            migrationBuilder.RenameTable(
                name: "Match",
                newName: "Matches");

            migrationBuilder.RenameIndex(
                name: "IX_Match_TeamBId",
                table: "Matches",
                newName: "IX_Matches_TeamBId");

            migrationBuilder.RenameIndex(
                name: "IX_Match_TeamAId",
                table: "Matches",
                newName: "IX_Matches_TeamAId");

            migrationBuilder.RenameIndex(
                name: "IX_Match_StandingId",
                table: "Matches",
                newName: "IX_Matches_StandingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Matches",
                table: "Matches",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "TeamsTournaments",
                columns: table => new
                {
                    TeamsId = table.Column<long>(type: "INTEGER", nullable: false),
                    TournamentsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamsTournaments", x => new { x.TeamsId, x.TournamentsId });
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Matches_MatchId",
                table: "Games",
                column: "MatchId",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Standings_StandingId",
                table: "Matches",
                column: "StandingId",
                principalTable: "Standings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamAId",
                table: "Matches",
                column: "TeamAId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_TeamBId",
                table: "Matches",
                column: "TeamBId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Matches_MatchId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Standings_StandingId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamAId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_TeamBId",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "TeamsTournaments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Matches",
                table: "Matches");

            migrationBuilder.RenameTable(
                name: "Matches",
                newName: "Match");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_TeamBId",
                table: "Match",
                newName: "IX_Match_TeamBId");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_TeamAId",
                table: "Match",
                newName: "IX_Match_TeamAId");

            migrationBuilder.RenameIndex(
                name: "IX_Matches_StandingId",
                table: "Match",
                newName: "IX_Match_StandingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Match",
                table: "Match",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Match_MatchId",
                table: "Games",
                column: "MatchId",
                principalTable: "Match",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Match_Standings_StandingId",
                table: "Match",
                column: "StandingId",
                principalTable: "Standings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Match_Teams_TeamAId",
                table: "Match",
                column: "TeamAId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Match_Teams_TeamBId",
                table: "Match",
                column: "TeamBId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
