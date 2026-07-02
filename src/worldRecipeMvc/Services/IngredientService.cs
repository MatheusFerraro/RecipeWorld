using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using worldRecipeMvc.Services.Errors;

namespace worldRecipeMvc.Services
{
    public class IngredientService : IIngredientService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IngredientService> _logger;

        public IngredientService(ApplicationDbContext context, IMemoryCache cache, ILogger<IngredientService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<PagedResult<Ingredient>>> GetIngredientsAsync(int pageNumber, int pageSize, string? searchTerm, bool approvedOnly = false)
        {
            var query = _context.Ingredients.AsNoTracking().AsQueryable();

            if (approvedOnly)
            {
                query = query.Where(i => i.IsApproved == true);
            }

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

            return Result.Ok(new PagedResult<Ingredient>
            {
                Items = ingredients,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        public async Task<Result<Ingredient>> GetIngredientByIdAsync(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            return ingredient == null
                ? Result.Fail(new NotFoundError(nameof(Ingredient), id))
                : Result.Ok(ingredient);
        }

        public async Task<Result<Ingredient>> CreateIngredientAsync(Ingredient ingredient, string userId)
        {
            if (await IngredientNameExistsAsync(ingredient.IngredientName!))
            {
                return Result.Fail(new ConflictError($"An Ingredient with the name {ingredient.IngredientName} already exists"));
            }

            var newIngredient = new Ingredient
            {
                IngredientName = ingredient.IngredientName,
                IngredientType = ingredient.IngredientType,
                IngredientDetails = ingredient.IngredientDetails,
                IsApproved = null,
                OwnerID = userId
            };

            _context.Ingredients.Add(newIngredient);
            await _context.SaveChangesAsync();
            _cache.Remove(CacheKeys.AllIngredients);

            _logger.LogInformation("Ingredient '{IngredientName}' created with ID {IngredientId} by user {UserId}",
                newIngredient.IngredientName, newIngredient.IngredientID, userId);
            return Result.Ok(newIngredient);
        }

        public async Task<Result> UpdateIngredientAsync(int id, Ingredient input, string userId, bool isAdmin)
        {
            var existing = await _context.Ingredients.FindAsync(id);
            if (existing == null)
            {
                return Result.Fail(new NotFoundError(nameof(Ingredient), id));
            }

            if (!CanModify(existing, userId, isAdmin))
            {
                return Result.Fail(new ForbiddenError("You can only edit your own unapproved ingredients."));
            }

            if (await IngredientNameExistsAsync(input.IngredientName!, id))
            {
                return Result.Fail(new ConflictError($"An Ingredient with the name {input.IngredientName} already exists"));
            }

            existing.IngredientName = input.IngredientName;
            existing.IngredientType = input.IngredientType;
            existing.IngredientDetails = input.IngredientDetails;
            // Any edit sends the ingredient back through the approval workflow
            existing.IsApproved = null;

            await _context.SaveChangesAsync();
            _cache.Remove(CacheKeys.AllIngredients);
            _logger.LogInformation("Ingredient {IngredientId} '{IngredientName}' updated", id, existing.IngredientName);
            return Result.Ok();
        }

        public async Task<Result> DeleteIngredientAsync(int id, string userId, bool isAdmin)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return Result.Fail(new NotFoundError(nameof(Ingredient), id));
            }

            if (!CanModify(ingredient, userId, isAdmin))
            {
                return Result.Fail(new ForbiddenError("You can only delete your own unapproved ingredients."));
            }

            var usedByRecipes = await _context.RecipeIngredients.AnyAsync(ri => ri.IngredientID == id);
            if (usedByRecipes)
            {
                return Result.Fail(new ConflictError($"Cannot delete ingredient '{ingredient.IngredientName}' because it is used by one or more recipes."));
            }

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();
            _cache.Remove(CacheKeys.AllIngredients);
            _logger.LogInformation("Ingredient {IngredientId} '{IngredientName}' deleted", id, ingredient.IngredientName);
            return Result.Ok();
        }

        public async Task<Result> SetApprovalAsync(int id, bool isApproved)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return Result.Fail(new NotFoundError(nameof(Ingredient), id));
            }

            ingredient.IsApproved = isApproved;
            await _context.SaveChangesAsync();
            _cache.Remove(CacheKeys.AllIngredients);
            _logger.LogInformation("Ingredient {IngredientId} '{IngredientName}' approval set to {IsApproved}",
                id, ingredient.IngredientName, isApproved);
            return Result.Ok();
        }

        public async Task<bool> IngredientNameExistsAsync(string ingredientName, int? excludeId = null)
        {
            var query = _context.Ingredients.Where(i => i.IngredientName!.ToUpper() == ingredientName.ToUpper());

            if (excludeId.HasValue)
            {
                query = query.Where(i => i.IngredientID != excludeId);
            }

            return await query.AnyAsync();
        }

        public async Task<List<Ingredient>> GetAllIngredientsAsync()
        {
            return (await _cache.GetOrCreateAsync(CacheKeys.AllIngredients, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheKeys.DropdownTtl;
                return _context.Ingredients.AsNoTracking()
                    .OrderBy(i => i.IngredientName)
                    .ToListAsync();
            }))!;
        }

        private static bool CanModify(Ingredient ingredient, string userId, bool isAdmin) =>
            isAdmin || (ingredient.OwnerID == userId && ingredient.IsApproved != true);
    }
}
