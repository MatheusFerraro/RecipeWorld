using worldRecipeMvc.Models;
using worldRecipeMvc.Models.ViewModels;

namespace worldRecipeMvc.Services
{
    public interface IRecipeService
    {
        Task<(IEnumerable<DisplayRecipeViewModel> Recipes, int TotalCount)> GetRecipesAsync(int pageNumber, int pageSize, string? searchTerm, string? categoryFilter, string? statusFilter);
        Task<Recipe?> GetRecipeByIdAsync(int id);
        Task<Recipe?> GetRecipeWithDetailsAsync(int id);
        Task<bool> CreateRecipeAsync(CreateRecipeViewModel viewModel, string userId);
        Task<bool> UpdateRecipeAsync(Recipe recipe, List<RecipeIngredient> ingredients);
        Task<bool> DeleteRecipeAsync(int id);
        Task<bool> RecipeExistsAsync(string recipeName, int? excludeId = null);
        Task<bool> UpdateRecipeStatusAsync(int id, string status);
    }
}
