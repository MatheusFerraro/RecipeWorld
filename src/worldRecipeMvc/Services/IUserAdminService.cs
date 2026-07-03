using FluentResults;
using worldRecipeMvc.Models;
using worldRecipeMvc.Models.ViewModels;

namespace worldRecipeMvc.Services
{
    public interface IUserAdminService
    {
        /// <summary>Paged user list with roles, activity, and lockout state (newest accounts first).</summary>
        Task<Result<PagedResult<UserAdminListItem>>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm);

        /// <summary>Locks the account indefinitely and invalidates existing sessions. Admins cannot lock themselves.</summary>
        Task<Result> LockAsync(string userId, string actingUserId);

        /// <summary>Clears the lockout and resets the failed-attempt counter.</summary>
        Task<Result> UnlockAsync(string userId);

        /// <summary>
        /// Deletes the account. The user's favorites and ratings are removed and
        /// ownership of their recipes/categories/ingredients is cleared (content stays).
        /// Admins cannot delete themselves.
        /// </summary>
        Task<Result> DeleteAsync(string userId, string actingUserId);
    }
}
