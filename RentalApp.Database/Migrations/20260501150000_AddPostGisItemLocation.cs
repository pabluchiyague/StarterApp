using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPostGisItemLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");
            migrationBuilder.Sql("""
                ALTER TABLE items
                    ADD COLUMN IF NOT EXISTS location geography(Point,4326);
                """);
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_items_location"
                    ON items
                    USING GIST (location);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_items_location",
                table: "items");

            migrationBuilder.DropColumn(
                name: "location",
                table: "items");
        }
    }
}
