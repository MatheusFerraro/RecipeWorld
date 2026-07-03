using FluentResults;
using worldRecipeMvc.Models;

namespace worldRecipeMvc.Services
{
    public record RatingSummary(double? AverageStars, int Count, Rating? CurrentUserRating);

    public interface IRatingService
    {
        /// <summary>
        /// Creates or updates the user's rating (one per user per recipe).
        /// Owners cannot rate their own recipes.
        /// </summary>
        Task<Result<Rating>> UpsertAsync(int recipeId, string userId, int stars, string? comment);

        /// <summary>Deletes a rating: the rating's author or an admin.</summary>
        Task<Result> DeleteAsync(int recipeId, string userId, bool isAdmin);

        /// <summary>Average/count plus the given user's own rating, if any.</summary>
        Task<RatingSummary> GetSummaryAsync(int recipeId, string? userId);

        /// <summary>Paged reviews for a recipe, newest first.</summary>
        Task<Result<PagedResult<Rating>>> GetReviewsAsync(int recipeId, int pageNumber, int pageSize);
    }
}
