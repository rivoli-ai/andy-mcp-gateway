using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpGateway.Infrastructure.Migrations
{
    /// <summary>
    /// Re-scopes API keys from per-user (OwnerSubject/OwnerEmail) to global-to-the-app
    /// (CreatedBy is purely audit metadata). Drops the per-owner index, drops both
    /// owner columns, and adds the new <c>CreatedBy</c> column.
    /// </summary>
    public partial class AlterApiKeysGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_api_keys_OwnerSubject",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "OwnerEmail",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "OwnerSubject",
                table: "api_keys");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "api_keys",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "api_keys");

            migrationBuilder.AddColumn<string>(
                name: "OwnerSubject",
                table: "api_keys",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerEmail",
                table: "api_keys",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_OwnerSubject",
                table: "api_keys",
                column: "OwnerSubject");
        }
    }
}
