using FluentResults;
using worldRecipeMvc.DTOs;
using worldRecipeMvc.Models;
using worldRecipeMvc.Models.ViewModels;

namespace worldRecipeMvc.Services
{
    public interface IRecipeService
    {
        /// <summary>
        /// Paged recipe list for the MVC index. When no status filter is given,
        /// visibility rules apply: anonymous users see Public recipes, signed-in
        /// users additionally see their own, admins see everything.
        /// </summary>
        Task<Result<PagedResult<DisplayRecipeViewModel>>> GetRecipesAsync(
            int pageNumber, int pageSize, string? searchTerm, int? categoryId,
            string? statusFilter, string? userId, bool isAdmin);

        /// <summary>Paged list of Public recipes projected to DTOs for the API.</summary>
        Task<Result<PagedResult<RecipeDto>>> GetPublicRecipesAsync(int pageNumber, int pageSize, string? searchTerm);

        /// <summary>Single Public recipe projected to a DTO for the API.</summary>
        Task<Result<RecipeDto>> GetPublicRecipeAsync(int id);

        /// <summary>Recipe with category/owner/ingredients loaded; no visibility check.</summary>
        Task<Result<Recipe>> GetRecipeWithDetailsAsync(int id);

        /// <summary>
        /// Like <see cref="GetRecipeWithDetailsAsync"/> but enforces that Draft and
        /// Private recipes are only visible to their owner or an admin.
        /// </summary>
        Task<Result<Recipe>> GetRecipeForViewingAsync(int id, string? userId, bool isAdmin);

        /// <summary>Creates a Draft recipe owned by <paramref name="userId"/> with de-duplicated ingredients.</summary>
        Task<Result<Recipe>> CreateRecipeAsync(Recipe recipe, IEnumerable<RecipeIngredient> ingredients, string userId);

        /// <summary>
        /// Updates scalar fields and replaces ingredients (pass null to leave
        /// ingredients untouched). The status is only applied when the caller is
        /// admin, or the owner and all linked ingredients/category are approved.
        /// </summary>
        Task<Result> UpdateRecipeAsync(int id, Recipe input, IEnumerable<RecipeIngredient>? ingredients, string userId, bool isAdmin);

        Task<Result> DeleteRecipeAsync(int id, string userId, bool isAdmin);

        /// <summary>Sets the recipe status to one of <see cref="RecipeStatus.All"/> (owner or admin).</summary>
        Task<Result> ChangeStatusAsync(int id, string status, string userId, bool isAdmin);

        Task<bool> RecipeNameExistsAsync(string recipeName, int? excludeId = null);

        /// <summary>
        /// Top-rated and most-favorited Public recipes for the home page.
        /// Cached for a few minutes (see <see cref="CacheKeys.TrendingTtl"/>).
        /// </summary>
        Task<HomeTrendingViewModel> GetTrendingAsync(int count = 4);
    }
}
