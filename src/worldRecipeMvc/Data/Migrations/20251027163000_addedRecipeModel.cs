using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace worldRecipeMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedRecipeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    RecipeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrepTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CookTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tips = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberOfServings = table.Column<double>(type: "float", nullable: true),
                    DefaultRecipeImage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.RecipeID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recipes");
        }
    }
}
