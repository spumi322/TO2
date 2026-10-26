using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TenantInviteCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Name",
                table: "Tenants");

            migrationBuilder.AddColumn<DateTime>(
                name: "InviteCodeExpiresAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteCodeHash",
                table: "Tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Same rule as Tenant.NormalizeName (trim, uppercase); names are stored trimmed
            migrationBuilder.Sql("""UPDATE "Tenants" SET "Name" = trim("Name"), "NormalizedName" = upper(trim("Name"));""");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_NormalizedName",
                table: "Tenants",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_NormalizedName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "InviteCodeExpiresAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "InviteCodeHash",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Tenants");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name",
                unique: true);
        }
    }
}
