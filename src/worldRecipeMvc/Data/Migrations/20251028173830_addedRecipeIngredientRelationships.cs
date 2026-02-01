using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace worldRecipeMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedRecipeIngredientRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_AspNetUsers_OwnerId",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Categories_CategoryID",
                table: "Recipes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecipeIngredients",
                table: "RecipeIngredients");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_RecipeID",
                table: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "RecipeIngredientID",
                table: "RecipeIngredients");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Recipes",
                newName: "OwnerID");

            migrationBuilder.RenameIndex(
                name: "IX_Recipes_OwnerId",
                table: "Recipes",
                newName: "IX_Recipes_OwnerID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecipeIngredients",
                table: "RecipeIngredients",
                columns: new[] { "RecipeID", "IngredientID" });

            migrationBuilder.AddForeignKey(
                name: "FK_Recipe_CategoryID",
                table: "Recipes",
                column: "CategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_AspNetUsers_OwnerID",
                table: "Recipes",
                column: "OwnerID",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipe_CategoryID",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_AspNetUsers_OwnerID",
                table: "Recipes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecipeIngredients",
                table: "RecipeIngredients");

            migrationBuilder.RenameColumn(
                name: "OwnerID",
                table: "Recipes",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Recipes_OwnerID",
                table: "Recipes",
                newName: "IX_Recipes_OwnerId");

            migrationBuilder.AddColumn<int>(
                name: "RecipeIngredientID",
                table: "RecipeIngredients",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecipeIngredients",
                table: "RecipeIngredients",
                column: "RecipeIngredientID");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeID",
                table: "RecipeIngredients",
                column: "RecipeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_AspNetUsers_OwnerId",
                table: "Recipes",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Categories_CategoryID",
                table: "Recipes",
                column: "CategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID");
        }
    }
}
