using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarterApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedHealthCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO categories (""Name"", ""ColorHex"", ""Description"")
                VALUES ('Health', '#9C27B0', 'Health and fitness notes');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM categories WHERE ""Name"" = 'Health';
            ");
        }
    }
}
