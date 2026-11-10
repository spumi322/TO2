using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TeamBelongsToTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EliminatedInRound",
                table: "Teams",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalPlacement",
                table: "Teams",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TournamentId",
                table: "Teams",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Every registration (TournamentTeams row) becomes one team of that tournament.
            // A team registered in several tournaments (rows from before teams became single-use) keeps its
            // first tournament; each other registration gets its own copy of the team.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE team_map AS
                SELECT tt."TournamentId" AS tournament_id, tt."TeamId" AS old_id,
                       CASE WHEN tt."TournamentId" = (SELECT min(x."TournamentId") FROM "TournamentTeams" x WHERE x."TeamId" = tt."TeamId")
                            THEN tt."TeamId"
                            ELSE nextval(pg_get_serial_sequence('"Teams"', 'Id'))
                       END AS new_id
                FROM "TournamentTeams" tt;

                INSERT INTO "Teams" ("Id", "Name", "TenantId", "CreatedDate", "LastModifiedDate", "CreatedBy", "LastModifiedBy", "Version", "TournamentId")
                SELECT m.new_id, t."Name", t."TenantId", t."CreatedDate", t."LastModifiedDate", t."CreatedBy", t."LastModifiedBy", t."Version", 0
                FROM team_map m JOIN "Teams" t ON t."Id" = m.old_id
                WHERE m.new_id <> m.old_id;

                UPDATE "Teams" t
                SET "TournamentId" = tt."TournamentId", "FinalPlacement" = tt."FinalPlacement", "EliminatedInRound" = tt."EliminatedInRound"
                FROM team_map m JOIN "TournamentTeams" tt ON tt."TournamentId" = m.tournament_id AND tt."TeamId" = m.old_id
                WHERE t."Id" = m.new_id;

                UPDATE "GroupEntries" g SET "TeamId" = m.new_id
                FROM team_map m
                WHERE m.new_id <> m.old_id AND g."TournamentId" = m.tournament_id AND g."TeamId" = m.old_id;
                """);

            // Matches and games of the other tournaments point to the copies
            foreach (var column in new[] { "TeamAId", "TeamBId", "WinnerId", "LoserId" })
            {
                migrationBuilder.Sql($"""
                    UPDATE "Matches" x SET "{column}" = m.new_id
                    FROM team_map m, "Standings" s
                    WHERE m.new_id <> m.old_id AND s."Id" = x."StandingId" AND s."TournamentId" = m.tournament_id AND x."{column}" = m.old_id;
                    """);
            }

            foreach (var column in new[] { "TeamAId", "TeamBId", "WinnerId" })
            {
                migrationBuilder.Sql($"""
                    UPDATE "Games" x SET "{column}" = m.new_id
                    FROM team_map m, "Standings" s, "Matches" mt
                    WHERE m.new_id <> m.old_id AND mt."Id" = x."MatchId" AND s."Id" = mt."StandingId" AND s."TournamentId" = m.tournament_id AND x."{column}" = m.old_id;
                    """);
            }

            // Teams that were in no tournament
            migrationBuilder.Sql("""
                DELETE FROM "Teams" WHERE "TournamentId" = 0;
                DROP TABLE team_map;
                """);

            migrationBuilder.DropTable(
                name: "TournamentTeams");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TournamentId",
                table: "Teams",
                column: "TournamentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Tournaments_TournamentId",
                table: "Teams",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TournamentTeams",
                columns: table => new
                {
                    TournamentId = table.Column<long>(type: "bigint", nullable: false),
                    TeamId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EliminatedInRound = table.Column<int>(type: "integer", nullable: true),
                    FinalPlacement = table.Column<int>(type: "integer", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResultFinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentTeams", x => new { x.TournamentId, x.TeamId });
                    table.ForeignKey(
                        name: "FK_TournamentTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentTeams_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentTeams_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TeamId",
                table: "TournamentTeams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TenantId",
                table: "TournamentTeams",
                column: "TenantId");

            // One registration per team; the copies made by Up stay separate teams
            migrationBuilder.Sql("""
                INSERT INTO "TournamentTeams" ("TournamentId", "TeamId", "TenantId", "CreatedBy", "CreatedDate", "EliminatedInRound", "FinalPlacement", "LastModifiedBy", "LastModifiedDate", "Version")
                SELECT "TournamentId", "Id", "TenantId", "CreatedBy", "CreatedDate", "EliminatedInRound", "FinalPlacement", "LastModifiedBy", "LastModifiedDate", 0
                FROM "Teams";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Tournaments_TournamentId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_TournamentId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "EliminatedInRound",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "FinalPlacement",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "TournamentId",
                table: "Teams");
        }
    }
}
