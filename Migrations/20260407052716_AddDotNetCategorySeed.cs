using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddDotNetCategorySeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert only if not already present (idempotent)
            migrationBuilder.Sql(@"
                INSERT INTO ""Categories"" (""Id"", ""ColorHex"", ""Description"", ""IconClass"", ""Name"")
                SELECT 7, '#8b5cf5', NULL, 'bi-code-slash', 'DotNet'
                WHERE NOT EXISTS (SELECT 1 FROM ""Categories"" WHERE ""Id"" = 7);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
