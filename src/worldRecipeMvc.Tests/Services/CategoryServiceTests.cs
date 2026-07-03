using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using worldRecipeMvc.Services;
using worldRecipeMvc.Services.Errors;
using Xunit;

namespace worldRecipeMvc.Tests.Services
{
    public class CategoryServiceTests : IDisposable
    {
        private const string OwnerId = "owner-1";
        private const string OtherUserId = "other-user";

        private readonly ApplicationDbContext _context;
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new CategoryService(_context, new MemoryCache(new MemoryCacheOptions()), Mock.Of<ILogger<CategoryService>>());

            SeedTestData();
        }

        private void SeedTestData()
        {
            var categories = new List<Category>
            {
                new Category { CategoryID = 1, CategoryName = "Desserts", CategoryDescription = "Sweet treats", IsApproved = true, OwnerID = OwnerId },
                new Category { CategoryID = 2, CategoryName = "Main Dishes", CategoryDescription = "Main courses", IsApproved = true, OwnerID = OwnerId },
                new Category { CategoryID = 3, CategoryName = "Appetizers", CategoryDescription = "Starters", IsApproved = null, OwnerID = OwnerId }
            };

            _context.Categories.AddRange(categories);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetCategoriesAsync_ReturnsPaginatedCategories()
        {
            var result = await _service.GetCategoriesAsync(pageNumber: 1, pageSize: 2, searchTerm: null, sortOrder: null);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Items.Count);
            Assert.Equal(3, result.Value.TotalCount);
            Assert.Equal(2, result.Value.TotalPages);
        }

        [Fact]
        public async Task GetCategoriesAsync_WithSearchTerm_ReturnsFilteredCategories()
        {
            var result = await _service.GetCategoriesAsync(pageNumber: 1, pageSize: 10, searchTerm: "Dessert", sortOrder: null);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Items);
            Assert.Equal("Desserts", result.Value.Items.First().CategoryName);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ReturnsCategory()
        {
            var result = await _service.GetCategoryByIdAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal("Desserts", result.Value.CategoryName);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ReturnsNotFoundForMissingCategory()
        {
            var result = await _service.GetCategoryByIdAsync(999);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<NotFoundError>());
        }

        [Fact]
        public async Task CreateCategoryAsync_CreatesPendingCategoryWithOwner()
        {
            var newCategory = new Category
            {
                CategoryName = "Soups",
                CategoryDescription = "Warm soups"
            };

            var result = await _service.CreateCategoryAsync(newCategory, OwnerId);

            Assert.True(result.IsSuccess);
            var savedCategory = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Soups");
            Assert.NotNull(savedCategory);
            Assert.Null(savedCategory!.IsApproved);
            Assert.Equal(OwnerId, savedCategory.OwnerID);
        }

        [Fact]
        public async Task CreateCategoryAsync_WithDuplicateName_ReturnsConflict()
        {
            var duplicateCategory = new Category
            {
                CategoryName = "Desserts",
                CategoryDescription = "Duplicate"
            };

            var result = await _service.CreateCategoryAsync(duplicateCategory, OwnerId);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ConflictError>());
        }

        [Fact]
        public async Task UpdateCategoryAsync_AsAdmin_UpdatesAndResetsApproval()
        {
            var input = new Category
            {
                CategoryID = 1,
                CategoryName = "Desserts Updated",
                CategoryDescription = "Updated description"
            };

            var result = await _service.UpdateCategoryAsync(1, input, OtherUserId, isAdmin: true);

            Assert.True(result.IsSuccess);
            var updated = await _context.Categories.FindAsync(1);
            Assert.Equal("Desserts Updated", updated!.CategoryName);
            Assert.Null(updated.IsApproved); // Any edit resets approval to pending
        }

        [Fact]
        public async Task UpdateCategoryAsync_OwnerOfApprovedCategory_ReturnsForbidden()
        {
            var input = new Category { CategoryID = 1, CategoryName = "Hijack", CategoryDescription = "x" };

            // Category 1 is approved: even the owner may no longer edit it
            var result = await _service.UpdateCategoryAsync(1, input, OwnerId, isAdmin: false);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ForbiddenError>());
        }

        [Fact]
        public async Task UpdateCategoryAsync_OwnerOfPendingCategory_Succeeds()
        {
            var input = new Category { CategoryID = 3, CategoryName = "Starters & Snacks", CategoryDescription = "y" };

            var result = await _service.UpdateCategoryAsync(3, input, OwnerId, isAdmin: false);

            Assert.True(result.IsSuccess);
            var updated = await _context.Categories.FindAsync(3);
            Assert.Equal("Starters & Snacks", updated!.CategoryName);
        }

        [Fact]
        public async Task UpdateCategoryAsync_NonOwner_ReturnsForbidden()
        {
            var input = new Category { CategoryID = 3, CategoryName = "Hijack", CategoryDescription = "x" };

            var result = await _service.UpdateCategoryAsync(3, input, OtherUserId, isAdmin: false);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ForbiddenError>());
        }

        [Fact]
        public async Task DeleteCategoryAsync_CategoryUsedByRecipe_ReturnsConflict()
        {
            _context.Recipes.Add(new Recipe { RecipeName = "Cake", Instructions = "Bake", CategoryID = 1, Status = RecipeStatus.Public });
            await _context.SaveChangesAsync();

            var result = await _service.DeleteCategoryAsync(1, OtherUserId, isAdmin: true);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ConflictError>());
        }

        [Fact]
        public async Task DeleteCategoryAsync_UnusedCategory_AsAdmin_Succeeds()
        {
            var result = await _service.DeleteCategoryAsync(2, OtherUserId, isAdmin: true);

            Assert.True(result.IsSuccess);
            Assert.Null(await _context.Categories.FindAsync(2));
        }

        [Fact]
        public async Task CategoryNameExistsAsync_ReturnsTrueForExistingCategory()
        {
            Assert.True(await _service.CategoryNameExistsAsync("Desserts"));
        }

        [Fact]
        public async Task CategoryNameExistsAsync_ReturnsFalseForNonExistingCategory()
        {
            Assert.False(await _service.CategoryNameExistsAsync("NonExistent"));
        }

        [Fact]
        public async Task SetApprovalAsync_UpdatesApprovalStatus()
        {
            var result = await _service.SetApprovalAsync(3, true);

            Assert.True(result.IsSuccess);
            var category = await _context.Categories.FindAsync(3);
            Assert.True(category!.IsApproved);
        }

        [Fact]
        public async Task GetApprovedCategoriesAsync_ReturnsOnlyApproved()
        {
            var categories = await _service.GetApprovedCategoriesAsync();

            Assert.Equal(2, categories.Count);
            Assert.All(categories, c => Assert.True(c.IsApproved));
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
