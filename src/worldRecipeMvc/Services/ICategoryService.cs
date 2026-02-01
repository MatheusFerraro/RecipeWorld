using worldRecipeMvc.Models;

namespace worldRecipeMvc.Services
{
    public interface ICategoryService
    {
        Task<(IEnumerable<Category> Categories, int TotalCount)> GetCategoriesAsync(int pageNumber, int pageSize, string? searchTerm, string? sortOrder);
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<bool> CreateCategoryAsync(Category category);
        Task<bool> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(int id);
        Task<bool> CategoryExistsAsync(string categoryName, int? excludeId = null);
        Task<bool> ApproveCategoryAsync(int id, bool isApproved);
        Task<List<Category>> GetAllCategoriesAsync();
    }
}
