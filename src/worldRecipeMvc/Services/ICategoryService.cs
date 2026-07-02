using FluentResults;
using worldRecipeMvc.Models;

namespace worldRecipeMvc.Services
{
    public interface ICategoryService
    {
        Task<Result<PagedResult<Category>>> GetCategoriesAsync(int pageNumber, int pageSize, string? searchTerm, string? sortOrder);

        Task<Result<Category>> GetCategoryByIdAsync(int id);

        /// <summary>Creates a pending (IsApproved = null) category owned by <paramref name="userId"/>.</summary>
        Task<Result<Category>> CreateCategoryAsync(Category category, string userId);

        /// <summary>
        /// Updates name/description and resets approval to pending. Only admins,
        /// or owners of not-yet-approved categories, may edit.
        /// </summary>
        Task<Result> UpdateCategoryAsync(int id, Category input, string userId, bool isAdmin);

        /// <summary>Deletes a category unless it is still referenced by recipes.</summary>
        Task<Result> DeleteCategoryAsync(int id, string userId, bool isAdmin);

        /// <summary>Admin approval toggle (true = approved, false = rejected).</summary>
        Task<Result> SetApprovalAsync(int id, bool isApproved);

        Task<bool> CategoryNameExistsAsync(string categoryName, int? excludeId = null);

        /// <summary>All categories ordered by name (recipe form dropdowns show pending items disabled).</summary>
        Task<List<Category>> GetAllCategoriesAsync();

        /// <summary>Approved categories only, for public filter dropdowns and the API.</summary>
        Task<List<Category>> GetApprovedCategoriesAsync();
    }
}
