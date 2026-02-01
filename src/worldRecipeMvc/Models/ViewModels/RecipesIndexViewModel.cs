

namespace worldRecipeMvc.Models.ViewModels
{
    public class RecipesIndexViewModel
    {
        public IEnumerable<DisplayRecipeViewModel> Recipes { get; set; } = new List<DisplayRecipeViewModel>();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
        
        // Filter properties
        public string? SearchTerm { get; set; }
        public int? CategoryFilter { get; set; }
        public string? StatusFilter { get; set; }
        
        // For dropdown
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    }
}
