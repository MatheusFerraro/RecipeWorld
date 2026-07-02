using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using worldRecipeMvc.Data;
using worldRecipeMvc.DTOs;
using worldRecipeMvc.Models;
using worldRecipeMvc.Models.ViewModels;
using worldRecipeMvc.Services.Errors;

namespace worldRecipeMvc.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RecipeService> _logger;

        public RecipeService(ApplicationDbContext context, IMemoryCache cache, ILogger<RecipeService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<PagedResult<DisplayRecipeViewModel>>> GetRecipesAsync(
            int pageNumber, int pageSize, string? searchTerm, int? categoryId,
            string? statusFilter, string? userId, bool isAdmin)
        {
            var query = _context.Recipes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => r.RecipeName!.Contains(searchTerm));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(r => r.CategoryID == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(r => r.Status == statusFilter);
            }
            else if (userId == null)
            {
                query = query.Where(r => r.Status == RecipeStatus.Public);
            }
            else if (!isAdmin)
            {
                query = query.Where(r => r.Status == RecipeStatus.Public || r.OwnerID == userId);
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
                    CategoryID = s.CategoryID,
                    Category = s.Category,
                    RecipeName = s.RecipeName,
                    PrepTime = TimeConversion(s.PrepTime),
                    CookTime = TimeConversion(s.CookTime),
                    Tips = s.Tips,
                    NumberOfServings = s.NumberOfServings,
                    Status = s.Status,
                    OwnerID = s.OwnerID,
                    Owner = s.Owner,
                    Instructions = s.Instructions,
                    Temperature = s.Temperature,
                    ImageUrl = s.ImageUrl,
                    AverageRating = s.Ratings.Any() ? s.Ratings.Average(rt => rt.Stars) : null,
                    RatingCount = s.Ratings.Count(),
                    FavoriteCount = s.Favorites.Count(),
                    IsFavorited = userId != null && s.Favorites.Any(f => f.UserId == userId)
                })
                .ToListAsync();

            return Result.Ok(new PagedResult<DisplayRecipeViewModel>
            {
                Items = recipes,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        public async Task<Result<PagedResult<RecipeDto>>> GetPublicRecipesAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var query = _context.Recipes.AsNoTracking()
                .Where(r => r.Status == RecipeStatus.Public);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => r.RecipeName!.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            var recipes = await query
                .OrderByDescending(r => r.RecipeID)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(RecipeDtoProjection)
                .ToListAsync();

            return Result.Ok(new PagedResult<RecipeDto>
            {
                Items = recipes,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        public async Task<Result<RecipeDto>> GetPublicRecipeAsync(int id)
        {
            var recipe = await _context.Recipes.AsNoTracking()
                .Where(r => r.RecipeID == id && r.Status == RecipeStatus.Public)
                .Select(RecipeDtoProjection)
                .FirstOrDefaultAsync();

            return recipe == null
                ? Result.Fail(new NotFoundError(nameof(Recipe), id))
                : Result.Ok(recipe);
        }

        public async Task<Result<Recipe>> GetRecipeWithDetailsAsync(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Owner)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(m => m.RecipeID == id);

            return recipe == null
                ? Result.Fail(new NotFoundError(nameof(Recipe), id))
                : Result.Ok(recipe);
        }

        public async Task<Result<Recipe>> GetRecipeForViewingAsync(int id, string? userId, bool isAdmin)
        {
            var result = await GetRecipeWithDetailsAsync(id);
            if (result.IsFailed)
            {
                return result;
            }

            var recipe = result.Value;
            if (recipe.Status == RecipeStatus.Draft || recipe.Status == RecipeStatus.Private)
            {
                if (!(isAdmin || (userId != null && recipe.OwnerID == userId)))
                {
                    return Result.Fail(new ForbiddenError("Only the owner or an admin can view this recipe."));
                }
            }

            return result;
        }

        public async Task<Result<Recipe>> CreateRecipeAsync(Recipe recipe, IEnumerable<RecipeIngredient> ingredients, string userId)
        {
            if (await RecipeNameExistsAsync(recipe.RecipeName!))
            {
                return Result.Fail(new ConflictError($"A Recipe with the name {recipe.RecipeName} already exists"));
            }

            recipe.Status = RecipeStatus.Draft;
            recipe.OwnerID = userId;
            if (string.IsNullOrWhiteSpace(recipe.ImageUrl))
            {
                recipe.ImageUrl = ImageStorageService.DefaultImageUrl;
            }

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            foreach (var ingredient in DeduplicateIngredients(recipe.RecipeID!.Value, ingredients))
            {
                _context.RecipeIngredients.Add(ingredient);
            }
            await _context.SaveChangesAsync();

            _logger.LogInformation("Recipe {RecipeName} created by user {UserId}", recipe.RecipeName, userId);
            return Result.Ok(recipe);
        }

        public async Task<Result> UpdateRecipeAsync(int id, Recipe input, IEnumerable<RecipeIngredient>? ingredients, string userId, bool isAdmin)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.RecipeID == id);

            if (recipe == null)
            {
                return Result.Fail(new NotFoundError(nameof(Recipe), id));
            }

            bool isOwner = recipe.OwnerID == userId;
            if (!(isAdmin || isOwner))
            {
                return Result.Fail(new ForbiddenError("Only the owner or an admin can edit this recipe."));
            }

            if (await RecipeNameExistsAsync(input.RecipeName!, id))
            {
                return Result.Fail(new ConflictError($"A Recipe with the name {input.RecipeName} already exists"));
            }

            recipe.RecipeName = input.RecipeName;
            recipe.CategoryID = input.CategoryID;
            recipe.PrepTime = input.PrepTime;
            recipe.CookTime = input.CookTime;
            recipe.Tips = input.Tips;
            recipe.NumberOfServings = input.NumberOfServings;
            recipe.Instructions = input.Instructions;
            recipe.Temperature = input.Temperature;

            if (!string.IsNullOrWhiteSpace(input.ImageUrl))
            {
                recipe.ImageUrl = input.ImageUrl;
            }

            if (!string.IsNullOrEmpty(input.Status) && input.Status != recipe.Status && CanChangeStatus(recipe, isAdmin, isOwner))
            {
                recipe.Status = input.Status;
            }

            if (ingredients != null)
            {
                _context.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
                foreach (var ingredient in DeduplicateIngredients(id, ingredients))
                {
                    _context.RecipeIngredients.Add(ingredient);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Recipe {RecipeId} updated by user {UserId}", id, userId);
            return Result.Ok();
        }

        public async Task<Result> DeleteRecipeAsync(int id, string userId, bool isAdmin)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return Result.Fail(new NotFoundError(nameof(Recipe), id));
            }

            if (!(isAdmin || recipe.OwnerID == userId))
            {
                return Result.Fail(new ForbiddenError("Only the owner or an admin can delete this recipe."));
            }

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Recipe {RecipeId} deleted by user {UserId}", id, userId);
            return Result.Ok();
        }

        public async Task<Result> ChangeStatusAsync(int id, string status, string userId, bool isAdmin)
        {
            if (!RecipeStatus.All.Contains(status))
            {
                return Result.Fail(new ValidationError("Invalid status selection.", nameof(Recipe.Status)));
            }

            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return Result.Fail(new NotFoundError(nameof(Recipe), id));
            }

            if (!(isAdmin || recipe.OwnerID == userId))
            {
                return Result.Fail(new ForbiddenError("Only the owner or an admin can change this recipe's status."));
            }

            recipe.Status = status;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Recipe {RecipeId} status changed to {Status} by user {UserId}", id, status, userId);
            return Result.Ok();
        }

        public async Task<bool> RecipeNameExistsAsync(string recipeName, int? excludeId = null)
        {
            var query = _context.Recipes.Where(r => r.RecipeName!.ToUpper() == recipeName.ToUpper());

            if (excludeId.HasValue)
            {
                query = query.Where(r => r.RecipeID != excludeId);
            }

            return await query.AnyAsync();
        }

        public async Task<HomeTrendingViewModel> GetTrendingAsync(int count = 4)
        {
            return (await _cache.GetOrCreateAsync(CacheKeys.HomeTrending, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheKeys.TrendingTtl;

                var publicRecipes = _context.Recipes.AsNoTracking()
                    .Where(r => r.Status == RecipeStatus.Public);

                var topRated = await publicRecipes
                    .Where(r => r.Ratings.Any())
                    .OrderByDescending(r => r.Ratings.Average(rt => rt.Stars))
                    .ThenByDescending(r => r.Ratings.Count())
                    .Take(count)
                    .Select(TrendingProjection)
                    .ToListAsync();

                var mostFavorited = await publicRecipes
                    .Where(r => r.Favorites.Any())
                    .OrderByDescending(r => r.Favorites.Count())
                    .Take(count)
                    .Select(TrendingProjection)
                    .ToListAsync();

                return new HomeTrendingViewModel
                {
                    TopRated = topRated,
                    MostFavorited = mostFavorited
                };
            }))!;
        }

        private static readonly System.Linq.Expressions.Expression<Func<Recipe, TrendingRecipeViewModel>> TrendingProjection = r => new TrendingRecipeViewModel
        {
            RecipeID = r.RecipeID,
            RecipeName = r.RecipeName,
            ImageUrl = r.ImageUrl,
            CategoryName = r.Category!.CategoryName,
            AverageRating = r.Ratings.Any() ? r.Ratings.Average(rt => rt.Stars) : null,
            RatingCount = r.Ratings.Count(),
            FavoriteCount = r.Favorites.Count()
        };

        /// <summary>
        /// Owners may only publish once every linked ingredient and the category
        /// have been approved; admins always can.
        /// </summary>
        private static bool CanChangeStatus(Recipe recipe, bool isAdmin, bool isOwner)
        {
            if (isAdmin)
            {
                return true;
            }

            if (!isOwner)
            {
                return false;
            }

            bool allIngredientsApproved = recipe.RecipeIngredients.All(ri => ri.Ingredient != null && ri.Ingredient.IsApproved == true);
            bool categoryApproved = recipe.Category == null || recipe.Category.IsApproved == true;
            return allIngredientsApproved && categoryApproved;
        }

        private static IEnumerable<RecipeIngredient> DeduplicateIngredients(int recipeId, IEnumerable<RecipeIngredient> ingredients)
        {
            var seen = new HashSet<int>();
            foreach (var ingredient in ingredients)
            {
                if (ingredient.IngredientID is > 0 && ingredient.Amount is > 0 && seen.Add(ingredient.IngredientID.Value))
                {
                    yield return new RecipeIngredient
                    {
                        RecipeID = recipeId,
                        IngredientID = ingredient.IngredientID,
                        Amount = ingredient.Amount,
                        Unit = ingredient.Unit
                    };
                }
            }
        }

        private static readonly System.Linq.Expressions.Expression<Func<Recipe, RecipeDto>> RecipeDtoProjection = r => new RecipeDto
        {
            RecipeID = r.RecipeID,
            RecipeName = r.RecipeName,
            CategoryID = r.CategoryID,
            CategoryName = r.Category!.CategoryName,
            PrepTime = r.PrepTime,
            CookTime = r.CookTime,
            Tips = r.Tips,
            NumberOfServings = r.NumberOfServings,
            Status = r.Status,
            Instructions = r.Instructions,
            ImageUrl = r.ImageUrl,
            Temperature = r.Temperature,
            OwnerName = r.Owner!.UserName
        };

        public static string? TimeConversion(int? time)
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
