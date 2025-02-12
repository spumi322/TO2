using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamNameToTournamentParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TeamName",
                table: "TournamentParticipants",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE TournamentParticipants
                SET TeamName = T.Name
                FROM Teams T
                WHERE T.Id = TournamentParticipants.TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamName",
                table: "TournamentParticipants");
        }
    }
}
