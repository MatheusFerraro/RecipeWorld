using FluentResults;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using worldRecipeMvc.Models.ViewModels;
using worldRecipeMvc.Services.Errors;

namespace worldRecipeMvc.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FavoriteService> _logger;

        public FavoriteService(ApplicationDbContext context, ILogger<FavoriteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<bool>> ToggleAsync(int recipeId, string userId)
        {
            var recipeExists = await _context.Recipes.AnyAsync(r => r.RecipeID == recipeId);
            if (!recipeExists)
            {
                return Result.Fail(new NotFoundError(nameof(Recipe), recipeId));
            }

            var existing = await _context.Favorites.FindAsync(userId, recipeId);
            if (existing != null)
            {
                _context.Favorites.Remove(existing);
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} unfavorited recipe {RecipeId}", userId, recipeId);
                return Result.Ok(false);
            }

            _context.Favorites.Add(new Favorite { UserId = userId, RecipeID = recipeId });
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} favorited recipe {RecipeId}", userId, recipeId);
            return Result.Ok(true);
        }

        public async Task<Result<PagedResult<DisplayRecipeViewModel>>> GetMyFavoritesAsync(string userId, int pageNumber, int pageSize)
        {
            var query = _context.Favorites.AsNoTracking()
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAtUtc);

            var totalCount = await query.CountAsync();

            var recipes = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new DisplayRecipeViewModel
                {
                    RecipeID = f.Recipe!.RecipeID,
                    CategoryID = f.Recipe.CategoryID,
                    Category = f.Recipe.Category,
                    RecipeName = f.Recipe.RecipeName,
                    PrepTime = RecipeService.TimeConversion(f.Recipe.PrepTime),
                    CookTime = RecipeService.TimeConversion(f.Recipe.CookTime),
                    Tips = f.Recipe.Tips,
                    NumberOfServings = f.Recipe.NumberOfServings,
                    Status = f.Recipe.Status,
                    OwnerID = f.Recipe.OwnerID,
                    Owner = f.Recipe.Owner,
                    Instructions = f.Recipe.Instructions,
                    Temperature = f.Recipe.Temperature,
                    ImageUrl = f.Recipe.ImageUrl,
                    AverageRating = f.Recipe.Ratings.Any() ? f.Recipe.Ratings.Average(rt => rt.Stars) : null,
                    RatingCount = f.Recipe.Ratings.Count(),
                    FavoriteCount = f.Recipe.Favorites.Count(),
                    IsFavorited = true
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

        public async Task<FavoriteInfo> GetInfoAsync(int recipeId, string? userId)
        {
            var count = await _context.Favorites.CountAsync(f => f.RecipeID == recipeId);
            var isFavorited = userId != null &&
                await _context.Favorites.AnyAsync(f => f.RecipeID == recipeId && f.UserId == userId);

            return new FavoriteInfo(count, isFavorited);
        }
    }
}
