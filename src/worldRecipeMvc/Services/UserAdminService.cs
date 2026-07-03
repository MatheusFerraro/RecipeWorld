using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using worldRecipeMvc.Models.ViewModels;
using worldRecipeMvc.Services.Errors;

namespace worldRecipeMvc.Services
{
    public class UserAdminService : IUserAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<RecipeWorldUser> _userManager;
        private readonly ILogger<UserAdminService> _logger;

        public UserAdminService(ApplicationDbContext context, UserManager<RecipeWorldUser> userManager, ILogger<UserAdminService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Result<PagedResult<UserAdminListItem>>> GetUsersAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u => u.Email!.Contains(searchTerm) ||
                                         u.DisplayName!.Contains(searchTerm) ||
                                         u.FirstName!.Contains(searchTerm) ||
                                         u.LastName!.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            var now = DateTimeOffset.UtcNow;
            var users = await query
                .OrderByDescending(u => u.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserAdminListItem
                {
                    Id = u.Id,
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    CreatedAtUtc = u.CreatedAtUtc,
                    LastLoginAtUtc = u.LastLoginAtUtc,
                    IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > now,
                    RecipeCount = u.Recipes!.Count(),
                    Roles = (from userRole in _context.UserRoles
                             join role in _context.Roles on userRole.RoleId equals role.Id
                             where userRole.UserId == u.Id
                             select role.Name!).ToList()
                })
                .ToListAsync();

            return Result.Ok(new PagedResult<UserAdminListItem>
            {
                Items = users,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        public async Task<Result> LockAsync(string userId, string actingUserId)
        {
            if (userId == actingUserId)
            {
                return Result.Fail(new ValidationError("You cannot lock your own account."));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result.Fail(new NotFoundError("User", userId));
            }

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            // Rotating the security stamp invalidates the user's existing sessions
            await _userManager.UpdateSecurityStampAsync(user);

            _logger.LogWarning("User {UserId} ({Email}) locked by admin {AdminId}", userId, user.Email, actingUserId);
            return Result.Ok();
        }

        public async Task<Result> UnlockAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result.Fail(new NotFoundError("User", userId));
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            _logger.LogInformation("User {UserId} ({Email}) unlocked", userId, user.Email);
            return Result.Ok();
        }

        public async Task<Result> DeleteAsync(string userId, string actingUserId)
        {
            if (userId == actingUserId)
            {
                return Result.Fail(new ValidationError("You cannot delete your own account."));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result.Fail(new NotFoundError("User", userId));
            }

            // The user's favorites/ratings block deletion (Restrict FKs), and the
            // Category/Ingredient owner FKs have no delete action — clear them so
            // the content itself survives without an owner.
            _context.Favorites.RemoveRange(_context.Favorites.Where(f => f.UserId == userId));
            _context.Ratings.RemoveRange(_context.Ratings.Where(r => r.UserId == userId));
            await _context.Categories.Where(c => c.OwnerID == userId)
                .ForEachAsync(c => c.OwnerID = null);
            await _context.Ingredients.Where(i => i.OwnerID == userId)
                .ForEachAsync(i => i.OwnerID = null);
            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return Result.Fail(new ValidationError(string.Join("; ", result.Errors.Select(e => e.Description))));
            }

            _logger.LogWarning("User {UserId} ({Email}) deleted by admin {AdminId}", userId, user.Email, actingUserId);
            return Result.Ok();
        }
    }
}
