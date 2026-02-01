using worldRecipeMvc.Models;

namespace worldRecipeMvc.Services
{
    public interface IIngredientService
    {
        Task<(IEnumerable<Ingredient> Ingredients, int TotalCount)> GetIngredientsAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<Ingredient?> GetIngredientByIdAsync(int id);
        Task<bool> CreateIngredientAsync(Ingredient ingredient);
        Task<bool> UpdateIngredientAsync(Ingredient ingredient);
        Task<bool> DeleteIngredientAsync(int id);
        Task<bool> IngredientExistsAsync(string ingredientName, int? excludeId = null);
        Task<bool> ApproveIngredientAsync(int id, bool isApproved);
    }
}
