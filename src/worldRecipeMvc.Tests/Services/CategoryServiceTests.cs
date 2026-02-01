using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using worldRecipeMvc.Services;
using Xunit;

namespace worldRecipeMvc.Tests.Services
{
    public class CategoryServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<CategoryService>> _loggerMock;
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<CategoryService>>();
            _service = new CategoryService(_context, _loggerMock.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var categories = new List<Category>
            {
                new Category { CategoryID = 1, CategoryName = "Desserts", CategoryDescription = "Sweet treats", IsApproved = true },
                new Category { CategoryID = 2, CategoryName = "Main Dishes", CategoryDescription = "Main courses", IsApproved = true },
                new Category { CategoryID = 3, CategoryName = "Appetizers", CategoryDescription = "Starters", IsApproved = null }
            };

            _context.Categories.AddRange(categories);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetCategoriesAsync_ReturnsPaginatedCategories()
        {
            // Act
            var result = await _service.GetCategoriesAsync(pageNumber: 1, pageSize: 2, searchTerm: null, sortOrder: null);

            // Assert
            Assert.Equal(2, result.Categories.Count());
            Assert.Equal(3, result.TotalCount);
        }

        [Fact]
        public async Task GetCategoriesAsync_WithSearchTerm_ReturnsFilteredCategories()
        {
            // Act
            var result = await _service.GetCategoriesAsync(pageNumber: 1, pageSize: 10, searchTerm: "Dessert", sortOrder: null);

            // Assert
            Assert.Single(result.Categories);
            Assert.Equal("Desserts", result.Categories.First().CategoryName);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ReturnsCategory()
        {
            // Act
            var result = await _service.GetCategoryByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Desserts", result!.CategoryName);
        }

        [Fact]
        public async Task CreateCategoryAsync_CreatesNewCategory()
        {
            // Arrange
            var newCategory = new Category
            {
                CategoryName = "Soups",
                CategoryDescription = "Warm soups"
            };

            // Act
            var result = await _service.CreateCategoryAsync(newCategory);

            // Assert
            Assert.True(result);
            var savedCategory = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Soups");
            Assert.NotNull(savedCategory);
            Assert.Null(savedCategory!.IsApproved);
        }

        [Fact]
        public async Task CreateCategoryAsync_WithDuplicateName_ReturnsFalse()
        {
            // Arrange
            var duplicateCategory = new Category
            {
                CategoryName = "Desserts",
                CategoryDescription = "Duplicate"
            };

            // Act
            var result = await _service.CreateCategoryAsync(duplicateCategory);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateCategoryAsync_UpdatesExistingCategory()
        {
            // Arrange
            var category = new Category
            {
                CategoryID = 1,
                CategoryName = "Desserts Updated",
                CategoryDescription = "Updated description"
            };

            // Act
            var result = await _service.UpdateCategoryAsync(category);

            // Assert
            Assert.True(result);
            var updated = await _context.Categories.FindAsync(1);
            Assert.Equal("Desserts Updated", updated!.CategoryName);
            Assert.Null(updated.IsApproved); // Should be reset to null
        }

        [Fact]
        public async Task CategoryExistsAsync_ReturnsTrueForExistingCategory()
        {
            // Act
            var result = await _service.CategoryExistsAsync("Desserts");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CategoryExistsAsync_ReturnsFalseForNonExistingCategory()
        {
            // Act
            var result = await _service.CategoryExistsAsync("NonExistent");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ApproveCategoryAsync_UpdatesApprovalStatus()
        {
            // Act
            var result = await _service.ApproveCategoryAsync(3, true);

            // Assert
            Assert.True(result);
            var category = await _context.Categories.FindAsync(3);
            Assert.True(category!.IsApproved);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
