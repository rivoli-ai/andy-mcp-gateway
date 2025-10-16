using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpGateway.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHeadersToAdapters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Headers",
                table: "mcp_adapters",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Headers",
                table: "mcp_adapters");
        }
    }
}
