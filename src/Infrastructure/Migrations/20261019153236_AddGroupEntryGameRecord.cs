using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupEntryGameRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameLosses",
                table: "GroupEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GameWins",
                table: "GroupEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill existing groups: count the games of the finished matches in the entry's group
            migrationBuilder.Sql("""
                UPDATE "GroupEntries" ge SET
                    "GameWins" = (
                        SELECT COUNT(*) FROM "Games" g JOIN "Matches" m ON m."Id" = g."MatchId"
                        WHERE m."StandingId" = ge."StandingId" AND m."WinnerId" IS NOT NULL
                          AND g."WinnerId" = ge."TeamId"),
                    "GameLosses" = (
                        SELECT COUNT(*) FROM "Games" g JOIN "Matches" m ON m."Id" = g."MatchId"
                        WHERE m."StandingId" = ge."StandingId" AND m."WinnerId" IS NOT NULL
                          AND ge."TeamId" IN (m."TeamAId", m."TeamBId")
                          AND g."WinnerId" IS NOT NULL AND g."WinnerId" <> ge."TeamId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameLosses",
                table: "GroupEntries");

            migrationBuilder.DropColumn(
                name: "GameWins",
                table: "GroupEntries");
        }
    }
}
