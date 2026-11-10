using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChampionTeamStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GroupsOnly no longer writes a status (its first place was Champion = 4, the rest Eliminated):
            // back to Competing = 1
            migrationBuilder.Sql(
                """
                UPDATE "GroupEntries" SET "Status" = 1
                WHERE "TournamentId" IN (SELECT "Id" FROM "Tournaments" WHERE "Format" = 3);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
