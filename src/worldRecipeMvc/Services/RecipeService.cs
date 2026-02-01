using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models.ViewModels;
using worldRecipeMvc.Models;


namespace worldRecipeMvc.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecipeService> _logger;

        public RecipeService(ApplicationDbContext context, ILogger<RecipeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(IEnumerable<DisplayRecipeViewModel> Recipes, int TotalCount)> GetRecipesAsync(
            int pageNumber, int pageSize, string? searchTerm, string? categoryFilter, string? statusFilter)
        {
            try
            {
                var query = _context.Recipes.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(r => r.RecipeName!.Contains(searchTerm) || 
                                           r.Instructions!.Contains(searchTerm) || 
                                           r.Tips!.Contains(searchTerm));
                }

                if (!string.IsNullOrWhiteSpace(categoryFilter) && int.TryParse(categoryFilter, out int categoryId))
                {
                    query = query.Where(r => r.CategoryID == categoryId);
                }

                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    query = query.Where(r => r.Status == statusFilter);
                }

                var totalCount = await query.CountAsync();

                var recipes = await query
                    .Include(r => r.Category)
                    .Include(r => r.Owner)
                    .OrderByDescending(r => r.RecipeID)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new DisplayRecipeViewModel
                    {
                        RecipeID = s.RecipeID,
                        Category = s.Category,
                        RecipeName = s.RecipeName,
                        PrepTime = TimeConversion(s.PrepTime),
                        CookTime = TimeConversion(s.CookTime),
                        Tips = s.Tips,
                        NumberOfServings = s.NumberOfServings,
                        Status = s.Status,
                        Owner = s.Owner,
                        Instructions = s.Instructions,
                        Temperature = s.Temperature,
                        ImageUrl = s.ImageUrl
                    })
                    .ToListAsync();

                return (recipes, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recipes with pagination");
                throw;
            }
        }

        public async Task<Recipe?> GetRecipeByIdAsync(int id)
        {
            return await _context.Recipes.FindAsync(id);
        }

        public async Task<Recipe?> GetRecipeWithDetailsAsync(int id)
        {
            return await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Owner)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(m => m.RecipeID == id);
        }

        public async Task<bool> CreateRecipeAsync(CreateRecipeViewModel viewModel, string userId)
        {
            try
            {
                if (await RecipeExistsAsync(viewModel.Recipe.RecipeName!))
                {
                    return false;
                }

                var recipeToSave = viewModel.Recipe;
                recipeToSave.Status = "Draft";
                recipeToSave.ImageUrl = "/websiteImages/RecipeDefaultImage.png";
                recipeToSave.OwnerID = userId;

                _context.Add(recipeToSave);
                await _context.SaveChangesAsync();

                var addedIngredientsIds = new HashSet<int>();

                foreach (var ingredient in viewModel.Ingredients)
                {
                    if (ingredient.IngredientID.HasValue && ingredient.IngredientID.Value > 0 
                        && ingredient.Amount.HasValue && ingredient.Amount.Value > 0)
                    {
                        int currentIngredientId = ingredient.IngredientID.Value;

                        if (!addedIngredientsIds.Contains(currentIngredientId))
                        {
                            ingredient.RecipeID = recipeToSave.RecipeID.Value;
                            _context.Add(ingredient);
                            addedIngredientsIds.Add(currentIngredientId);
                        }
                    }
                }
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recipe {RecipeName} created successfully by user {UserId}", recipeToSave.RecipeName, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating recipe");
                return false;
            }
        }

        public async Task<bool> UpdateRecipeAsync(Recipe recipe, List<RecipeIngredient> ingredients)
        {
            try
            {
                _context.Update(recipe);

                var existingIngredients = await _context.RecipeIngredients
                    .Where(ri => ri.RecipeID == recipe.RecipeID)
                    .ToListAsync();

                _context.RecipeIngredients.RemoveRange(existingIngredients);

                foreach (var ingredient in ingredients.Where(i => i.IngredientID.HasValue && i.Amount.HasValue))
                {
                    _context.RecipeIngredients.Add(ingredient);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Recipe {RecipeId} updated successfully", recipe.RecipeID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating recipe {RecipeId}", recipe.RecipeID);
                return false;
            }
        }

        public async Task<bool> DeleteRecipeAsync(int id)
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null)
                {
                    return false;
                }

                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Recipe {RecipeId} deleted successfully", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe {RecipeId}", id);
                return false;
            }
        }

        public async Task<bool> RecipeExistsAsync(string recipeName, int? excludeId = null)
        {
            var query = _context.Recipes.Where(r => r.RecipeName!.ToUpper() == recipeName.ToUpper());
            
            if (excludeId.HasValue)
            {
                query = query.Where(r => r.RecipeID != excludeId);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> UpdateRecipeStatusAsync(int id, string status)
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null)
                {
                    return false;
                }

                recipe.Status = status;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Recipe {RecipeId} status updated to {Status}", id, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating recipe {RecipeId} status", id);
                return false;
            }
        }

        private static string? TimeConversion(int? time)
        {
            if (!time.HasValue) return null;

            int hours = time.Value / 60;
            int minutes = time.Value % 60;

            if (hours > 0 && minutes > 0)
            {
                return $"{hours} hr {minutes} min";
            }
            else if (hours > 0)
            {
                return $"{hours} hr";
            }
            else
            {
                return $"{minutes} min";
            }
        }
    }
}
