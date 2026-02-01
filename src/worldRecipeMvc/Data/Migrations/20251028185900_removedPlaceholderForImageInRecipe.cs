using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace worldRecipeMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class removedPlaceholderForImageInRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultRecipeImage",
                table: "Recipes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultRecipeImage",
                table: "Recipes",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
