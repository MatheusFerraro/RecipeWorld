using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;

namespace worldRecipeMvc.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ApplicationDbContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(IEnumerable<Category> Categories, int TotalCount)> GetCategoriesAsync(
            int pageNumber, int pageSize, string? searchTerm, string? sortOrder)
        {
            try
            {
                var query = _context.Categories.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(c => c.CategoryName!.Contains(searchTerm) || 
                                           c.CategoryDescription!.Contains(searchTerm));
                }

                switch (sortOrder)
                {
                    case "name_desc":
                        query = query.OrderByDescending(c => c.CategoryName);
                        break;
                    default:
                        query = query.OrderBy(c => c.CategoryName);
                        break;
                }

                var totalCount = await query.CountAsync();

                var categories = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();

                return (categories, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories with pagination");
                throw;
            }
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<bool> CreateCategoryAsync(Category category)
        {
            try
            {
                if (await CategoryExistsAsync(category.CategoryName!))
                {
                    return false;
                }

                category.IsApproved = null;
                _context.Add(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Category '{CategoryName}' created with ID {CategoryId}", 
                    category.CategoryName, category.CategoryID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return false;
            }
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            try
            {
                if (await CategoryExistsAsync(category.CategoryName!, category.CategoryID))
                {
                    return false;
                }

                var existing = await _context.Categories.FindAsync(category.CategoryID);
                if (existing == null)
                {
                    return false;
                }

                existing.CategoryName = category.CategoryName;
                existing.CategoryDescription = category.CategoryDescription;
                existing.IsApproved = null;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Category {CategoryId} '{CategoryName}' updated", 
                    category.CategoryID, category.CategoryName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", category.CategoryID);
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return false;
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Category {CategoryId} deleted successfully", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                return false;
            }
        }

        public async Task<bool> CategoryExistsAsync(string categoryName, int? excludeId = null)
        {
            var query = _context.Categories.Where(c => c.CategoryName!.ToUpper() == categoryName.ToUpper());
            
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.CategoryID != excludeId);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> ApproveCategoryAsync(int id, bool isApproved)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return false;
                }

                category.IsApproved = isApproved;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Category {CategoryId} '{CategoryName}' approval status set to {IsApproved}", 
                    id, category.CategoryName, isApproved);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId} approval status", id);
                return false;
            }
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync();
        }
    }
}
