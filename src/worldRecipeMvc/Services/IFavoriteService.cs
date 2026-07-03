using FluentResults;
using worldRecipeMvc.Models;
using worldRecipeMvc.Models.ViewModels;

namespace worldRecipeMvc.Services
{
    public record FavoriteInfo(int Count, bool IsFavoritedByCurrentUser);

    public interface IFavoriteService
    {
        /// <summary>Adds or removes the user's favorite; returns the new state (true = now favorited).</summary>
        Task<Result<bool>> ToggleAsync(int recipeId, string userId);

        /// <summary>Paged list of the user's favorited recipes, newest favorite first.</summary>
        Task<Result<PagedResult<DisplayRecipeViewModel>>> GetMyFavoritesAsync(string userId, int pageNumber, int pageSize);

        /// <summary>Favorite count for a recipe plus whether the given user favorited it.</summary>
        Task<FavoriteInfo> GetInfoAsync(int recipeId, string? userId);
    }
}
