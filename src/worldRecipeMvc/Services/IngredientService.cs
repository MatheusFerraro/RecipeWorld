using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;

namespace worldRecipeMvc.Services
{
    public class IngredientService : IIngredientService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IngredientService> _logger;

        public IngredientService(ApplicationDbContext context, ILogger<IngredientService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(IEnumerable<Ingredient> Ingredients, int TotalCount)> GetIngredientsAsync(
            int pageNumber, int pageSize, string? searchTerm)
        {
            try
            {
                var query = _context.Ingredients.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(i => i.IngredientName!.Contains(searchTerm) || 
                                           i.IngredientType!.Contains(searchTerm));
                }

                var totalCount = await query.CountAsync();

                var ingredients = await query
                    .OrderBy(i => i.IngredientName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (ingredients, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ingredients with pagination");
                throw;
            }
        }

        public async Task<Ingredient?> GetIngredientByIdAsync(int id)
        {
            return await _context.Ingredients.FindAsync(id);
        }

        public async Task<bool> CreateIngredientAsync(Ingredient ingredient)
        {
            try
            {
                if (await IngredientExistsAsync(ingredient.IngredientName!))
                {
                    return false;
                }

                ingredient.IsApproved = null;
                _context.Add(ingredient);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Ingredient '{IngredientName}' created with ID {IngredientId}", 
                    ingredient.IngredientName, ingredient.IngredientID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ingredient");
                return false;
            }
        }

        public async Task<bool> UpdateIngredientAsync(Ingredient ingredient)
        {
            try
            {
                if (await IngredientExistsAsync(ingredient.IngredientName!, ingredient.IngredientID))
                {
                    return false;
                }

                var existing = await _context.Ingredients.FindAsync(ingredient.IngredientID);
                if (existing == null)
                {
                    return false;
                }

                existing.IngredientName = ingredient.IngredientName;
                existing.IngredientType = ingredient.IngredientType;
                existing.IngredientDetails = ingredient.IngredientDetails;
                existing.IsApproved = null;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Ingredient {IngredientId} '{IngredientName}' updated", 
                    ingredient.IngredientID, ingredient.IngredientName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ingredient {IngredientId}", ingredient.IngredientID);
                return false;
            }
        }

        public async Task<bool> DeleteIngredientAsync(int id)
        {
            try
            {
                var ingredient = await _context.Ingredients.FindAsync(id);
                if (ingredient == null)
                {
                    return false;
                }

                _context.Ingredients.Remove(ingredient);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Ingredient {IngredientId} deleted successfully", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ingredient {IngredientId}", id);
                return false;
            }
        }

        public async Task<bool> IngredientExistsAsync(string ingredientName, int? excludeId = null)
        {
            var query = _context.Ingredients.Where(i => i.IngredientName!.ToUpper() == ingredientName.ToUpper());
            
            if (excludeId.HasValue)
            {
                query = query.Where(i => i.IngredientID != excludeId);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> ApproveIngredientAsync(int id, bool isApproved)
        {
            try
            {
                var ingredient = await _context.Ingredients.FindAsync(id);
                if (ingredient == null)
                {
                    return false;
                }

                ingredient.IsApproved = isApproved;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Ingredient {IngredientId} '{IngredientName}' approval status set to {IsApproved}", 
                    id, ingredient.IngredientName, isApproved);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ingredient {IngredientId} approval status", id);
                return false;
            }
        }
    }
}
