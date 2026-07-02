using System.ComponentModel.DataAnnotations;

namespace worldRecipeMvc.DTOs
{
    // ---- Output DTOs -------------------------------------------------------

    public class RecipeDto
    {
        public int? RecipeID { get; set; }
        public string? RecipeName { get; set; }
        public int? CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public int? PrepTime { get; set; }
        public int? CookTime { get; set; }
        public string? Tips { get; set; }
        public double? NumberOfServings { get; set; }
        public string? Status { get; set; }
        public string? Instructions { get; set; }
        public string? ImageUrl { get; set; }
        public int? Temperature { get; set; }
        public string? OwnerName { get; set; }
    }

    public class IngredientDto
    {
        public int? IngredientID { get; set; }
        public string? IngredientName { get; set; }
        public string? IngredientType { get; set; }
        public string? IngredientDetails { get; set; }
        public bool? IsApproved { get; set; }
    }

    public class CategoryDto
    {
        public int? CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryDescription { get; set; }
        public bool? IsApproved { get; set; }
    }

    // ---- Rating / Favorite DTOs --------------------------------------------

    public class RatingDto
    {
        public int RecipeID { get; set; }
        public int Stars { get; set; }
        public string? Comment { get; set; }
        public string? ReviewerName { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class CreateRatingDto
    {
        [Range(1, 5)]
        public int Stars { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }

    public class RatingSummaryDto
    {
        public int RecipeID { get; set; }
        public double? AverageStars { get; set; }
        public int Count { get; set; }
        public int? YourStars { get; set; }
    }

    public class FavoriteStatusDto
    {
        public int RecipeID { get; set; }
        public bool IsFavorited { get; set; }
        public int FavoriteCount { get; set; }
    }

    // ---- Auth DTOs ---------------------------------------------------------

    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; }
    }

    public class TokenResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }

    // ---- Input DTOs --------------------------------------------------------
    // Deliberately without ID/IsApproved/Owner fields so clients cannot
    // over-post server-controlled state.

    public class CreateRecipeDto
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string? RecipeName { get; set; }

        public int? CategoryID { get; set; }

        [Range(0, 1000)]
        public int? PrepTime { get; set; }

        [Range(0, 1000)]
        public int? CookTime { get; set; }

        public string? Tips { get; set; }

        [Range(0, 1000)]
        public double? NumberOfServings { get; set; }

        [Required]
        public string? Instructions { get; set; }

        [Range(0, 1000)]
        public int? Temperature { get; set; }
    }

    public class UpdateRecipeDto : CreateRecipeDto
    {
        /// <summary>Only applied when the caller is allowed to change status (admin, or owner with approved ingredients/category).</summary>
        public string? Status { get; set; }
    }

    public class CreateIngredientDto
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string? IngredientName { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string? IngredientType { get; set; }

        [StringLength(1000)]
        public string? IngredientDetails { get; set; }
    }
}
