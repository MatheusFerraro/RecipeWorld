using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace worldRecipeMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerToCategoriesAndIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerID",
                table: "Ingredients",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerID",
                table: "Categories",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_OwnerID",
                table: "Ingredients",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_OwnerID",
                table: "Categories",
                column: "OwnerID");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_OwnerID",
                table: "Categories",
                column: "OwnerID",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Ingredients_AspNetUsers_OwnerID",
                table: "Ingredients",
                column: "OwnerID",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AspNetUsers_OwnerID",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Ingredients_AspNetUsers_OwnerID",
                table: "Ingredients");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_OwnerID",
                table: "Ingredients");

            migrationBuilder.DropIndex(
                name: "IX_Categories_OwnerID",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "OwnerID",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "OwnerID",
                table: "Categories");
        }
    }
}
