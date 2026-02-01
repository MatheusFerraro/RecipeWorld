using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace worldRecipeMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedIsApprovedToIngredientModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Ingredients",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Ingredients");
        }
    }
}
