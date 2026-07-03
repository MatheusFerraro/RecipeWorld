using FluentResults;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using worldRecipeMvc.Services.Errors;

namespace worldRecipeMvc.Services
{
    public class RatingService : IRatingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RatingService> _logger;

        public RatingService(ApplicationDbContext context, ILogger<RatingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<Rating>> UpsertAsync(int recipeId, string userId, int stars, string? comment)
        {
            if (stars is < 1 or > 5)
            {
                return Result.Fail(new ValidationError("Rating must be between 1 and 5 stars.", nameof(Rating.Stars)));
            }

            var recipe = await _context.Recipes.FindAsync(recipeId);
            if (recipe == null)
            {
                return Result.Fail(new NotFoundError(nameof(Recipe), recipeId));
            }

            if (recipe.OwnerID == userId)
            {
                return Result.Fail(new ValidationError("You cannot rate your own recipe."));
            }

            var rating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.RecipeID == recipeId && r.UserId == userId);

            if (rating == null)
            {
                rating = new Rating
                {
                    RecipeID = recipeId,
                    UserId = userId,
                    Stars = stars,
                    Comment = comment
                };
                _context.Ratings.Add(rating);
            }
            else
            {
                rating.Stars = stars;
                rating.Comment = comment;
                rating.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} rated recipe {RecipeId} with {Stars} stars", userId, recipeId, stars);
            return Result.Ok(rating);
        }

        public async Task<Result> DeleteAsync(int recipeId, string userId, bool isAdmin)
        {
            var rating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.RecipeID == recipeId && r.UserId == userId);

            // Admins may also remove someone else's review by its author id;
            // for the common case the author deletes their own rating.
            if (rating == null)
            {
                return Result.Fail(new NotFoundError(nameof(Rating), $"{recipeId}/{userId}"));
            }

            if (rating.UserId != userId && !isAdmin)
            {
                return Result.Fail(new ForbiddenError("Only the review author or an admin can delete a review."));
            }

            _context.Ratings.Remove(rating);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Rating for recipe {RecipeId} by user {UserId} deleted", recipeId, userId);
            return Result.Ok();
        }

        public async Task<RatingSummary> GetSummaryAsync(int recipeId, string? userId)
        {
            var stats = await _context.Ratings.AsNoTracking()
                .Where(r => r.RecipeID == recipeId)
                .GroupBy(r => r.RecipeID)
                .Select(g => new { Average = g.Average(r => (double)r.Stars), Count = g.Count() })
                .FirstOrDefaultAsync();

            Rating? currentUserRating = null;
            if (userId != null)
            {
                currentUserRating = await _context.Ratings.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RecipeID == recipeId && r.UserId == userId);
            }

            return new RatingSummary(stats?.Average, stats?.Count ?? 0, currentUserRating);
        }

        public async Task<Result<PagedResult<Rating>>> GetReviewsAsync(int recipeId, int pageNumber, int pageSize)
        {
            var query = _context.Ratings.AsNoTracking()
                .Where(r => r.RecipeID == recipeId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAtUtc);

            var totalCount = await query.CountAsync();

            var reviews = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Result.Ok(new PagedResult<Rating>
            {
                Items = reviews,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }
    }
}
