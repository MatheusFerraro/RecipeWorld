namespace worldRecipeMvc.DTOs
{
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

    public class CreateRecipeDto
    {
        public string? RecipeName { get; set; }
        public int? CategoryID { get; set; }
        public int? PrepTime { get; set; }
        public int? CookTime { get; set; }
        public string? Tips { get; set; }
        public double? NumberOfServings { get; set; }
        public string? Instructions { get; set; }
        public int? Temperature { get; set; }
    }

    public class UpdateRecipeDto
    {
        public string? RecipeName { get; set; }
        public int? CategoryID { get; set; }
        public int? PrepTime { get; set; }
        public int? CookTime { get; set; }
        public string? Tips { get; set; }
        public double? NumberOfServings { get; set; }
        public string? Instructions { get; set; }
        public string? Status { get; set; }
        public int? Temperature { get; set; }
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

    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }
}
