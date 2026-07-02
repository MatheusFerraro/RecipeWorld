using FluentResults;
using worldRecipeMvc.Models;

namespace worldRecipeMvc.Services
{
    public interface IIngredientService
    {
        Task<Result<PagedResult<Ingredient>>> GetIngredientsAsync(int pageNumber, int pageSize, string? searchTerm, bool approvedOnly = false);

        Task<Result<Ingredient>> GetIngredientByIdAsync(int id);

        /// <summary>Creates a pending (IsApproved = null) ingredient owned by <paramref name="userId"/>.</summary>
        Task<Result<Ingredient>> CreateIngredientAsync(Ingredient ingredient, string userId);

        /// <summary>
        /// Updates name/type/details and resets approval to pending. Only admins,
        /// or owners of not-yet-approved ingredients, may edit.
        /// </summary>
        Task<Result> UpdateIngredientAsync(int id, Ingredient input, string userId, bool isAdmin);

        /// <summary>Deletes an ingredient unless it is still used by recipes.</summary>
        Task<Result> DeleteIngredientAsync(int id, string userId, bool isAdmin);

        /// <summary>Admin approval toggle (true = approved, false = rejected).</summary>
        Task<Result> SetApprovalAsync(int id, bool isApproved);

        Task<bool> IngredientNameExistsAsync(string ingredientName, int? excludeId = null);

        /// <summary>All ingredients ordered by name (recipe form dropdowns show pending items disabled).</summary>
        Task<List<Ingredient>> GetAllIngredientsAsync();
    }
}
