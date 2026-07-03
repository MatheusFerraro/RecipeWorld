namespace worldRecipeMvc.Models.ViewModels
{
    public class TrendingRecipeViewModel
    {
        public int? RecipeID { get; set; }
        public string? RecipeName { get; set; }
        public string? ImageUrl { get; set; }
        public string? CategoryName { get; set; }
        public double? AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int FavoriteCount { get; set; }
    }

    public class HomeTrendingViewModel
    {
        public List<TrendingRecipeViewModel> TopRated { get; set; } = new();
        public List<TrendingRecipeViewModel> MostFavorited { get; set; } = new();
    }
}
