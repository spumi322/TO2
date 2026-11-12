using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceRowVersionWithAppManagedVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Games");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "TournamentTeams",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Tournaments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Tenants",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Teams",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Standings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "RefreshTokens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Players",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Matches",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "GroupEntries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Games",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "TournamentTeams");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GroupEntries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Games");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Tournaments",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Standings",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Matches",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Games",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }
    }
}
