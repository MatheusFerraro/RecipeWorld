using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using worldRecipeMvc.Services.Errors;

namespace worldRecipeMvc.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ApplicationDbContext context, IMemoryCache cache, ILogger<CategoryService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<PagedResult<Category>>> GetCategoriesAsync(int pageNumber, int pageSize, string? searchTerm, string? sortOrder)
        {
            var query = _context.Categories.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.CategoryName!.Contains(searchTerm) ||
                                         c.CategoryDescription!.Contains(searchTerm));
            }

            query = sortOrder == "name_desc"
                ? query.OrderByDescending(c => c.CategoryName)
                : query.OrderBy(c => c.CategoryName);

            var totalCount = await query.CountAsync();

            var categories = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Result.Ok(new PagedResult<Category>
            {
                Items = categories,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        public async Task<Result<Category>> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            return category == null
                ? Result.Fail(new NotFoundError(nameof(Category), id))
                : Result.Ok(category);
        }

        public async Task<Result<Category>> CreateCategoryAsync(Category category, string userId)
        {
            if (await CategoryNameExistsAsync(category.CategoryName!))
            {
                return Result.Fail(new ConflictError($"A Category with the name {category.CategoryName} already exists"));
            }

            var newCategory = new Category
            {
                CategoryName = category.CategoryName,
                CategoryDescription = category.CategoryDescription,
                IsApproved = null,
                OwnerID = userId
            };

            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();
            EvictDropdownCache();

            _logger.LogInformation("Category '{CategoryName}' created with ID {CategoryId} by user {UserId}",
                newCategory.CategoryName, newCategory.CategoryID, userId);
            return Result.Ok(newCategory);
        }

        public async Task<Result> UpdateCategoryAsync(int id, Category input, string userId, bool isAdmin)
        {
            var existing = await _context.Categories.FindAsync(id);
            if (existing == null)
            {
                return Result.Fail(new NotFoundError(nameof(Category), id));
            }

            if (!CanModify(existing, userId, isAdmin))
            {
                return Result.Fail(new ForbiddenError("You can only edit your own unapproved categories."));
            }

            if (await CategoryNameExistsAsync(input.CategoryName!, id))
            {
                return Result.Fail(new ConflictError($"A Category with the name {input.CategoryName} already exists"));
            }

            existing.CategoryName = input.CategoryName;
            existing.CategoryDescription = input.CategoryDescription;
            // Any edit sends the category back through the approval workflow
            existing.IsApproved = null;

            await _context.SaveChangesAsync();
            EvictDropdownCache();
            _logger.LogInformation("Category {CategoryId} '{CategoryName}' updated", id, existing.CategoryName);
            return Result.Ok();
        }

        public async Task<Result> DeleteCategoryAsync(int id, string userId, bool isAdmin)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return Result.Fail(new NotFoundError(nameof(Category), id));
            }

            if (!CanModify(category, userId, isAdmin))
            {
                return Result.Fail(new ForbiddenError("You can only delete your own unapproved categories."));
            }

            var usedByRecipes = await _context.Recipes.AnyAsync(r => r.CategoryID == id);
            if (usedByRecipes)
            {
                return Result.Fail(new ConflictError($"Cannot delete category '{category.CategoryName}' because it is used by one or more recipes."));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            EvictDropdownCache();
            _logger.LogInformation("Category {CategoryId} '{CategoryName}' deleted", id, category.CategoryName);
            return Result.Ok();
        }

        public async Task<Result> SetApprovalAsync(int id, bool isApproved)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return Result.Fail(new NotFoundError(nameof(Category), id));
            }

            category.IsApproved = isApproved;
            await _context.SaveChangesAsync();
            EvictDropdownCache();
            _logger.LogInformation("Category {CategoryId} '{CategoryName}' approval set to {IsApproved}",
                id, category.CategoryName, isApproved);
            return Result.Ok();
        }

        public async Task<bool> CategoryNameExistsAsync(string categoryName, int? excludeId = null)
        {
            var query = _context.Categories.Where(c => c.CategoryName!.ToUpper() == categoryName.ToUpper());

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.CategoryID != excludeId);
            }

            return await query.AnyAsync();
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return (await _cache.GetOrCreateAsync(CacheKeys.AllCategories, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheKeys.DropdownTtl;
                return _context.Categories.AsNoTracking()
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();
            }))!;
        }

        public async Task<List<Category>> GetApprovedCategoriesAsync()
        {
            return (await _cache.GetOrCreateAsync(CacheKeys.ApprovedCategories, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheKeys.DropdownTtl;
                return _context.Categories.AsNoTracking()
                    .Where(c => c.IsApproved == true)
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();
            }))!;
        }

        private void EvictDropdownCache()
        {
            _cache.Remove(CacheKeys.AllCategories);
            _cache.Remove(CacheKeys.ApprovedCategories);
        }

        private static bool CanModify(Category category, string userId, bool isAdmin) =>
            isAdmin || (category.OwnerID == userId && category.IsApproved != true);
    }
}
