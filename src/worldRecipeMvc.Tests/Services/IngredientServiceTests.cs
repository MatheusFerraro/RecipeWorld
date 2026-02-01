using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using worldRecipeMvc.Services;
using Xunit;

namespace worldRecipeMvc.Tests.Services
{
    public class IngredientServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<IngredientService>> _loggerMock;
        private readonly IngredientService _service;

        public IngredientServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<IngredientService>>();
            _service = new IngredientService(_context, _loggerMock.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var ingredients = new List<Ingredient>
            {
                new Ingredient { IngredientID = 1, IngredientName = "Sugar", IngredientType = "Sweetener", IsApproved = true },
                new Ingredient { IngredientID = 2, IngredientName = "Flour", IngredientType = "Grain", IsApproved = true },
                new Ingredient { IngredientID = 3, IngredientName = "Salt", IngredientType = "Seasoning", IsApproved = null }
            };

            _context.Ingredients.AddRange(ingredients);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetIngredientsAsync_ReturnsPaginatedIngredients()
        {
            // Act
            var result = await _service.GetIngredientsAsync(pageNumber: 1, pageSize: 2, searchTerm: null);

            // Assert
            Assert.Equal(2, result.Ingredients.Count());
            Assert.Equal(3, result.TotalCount);
        }

        [Fact]
        public async Task GetIngredientsAsync_WithSearchTerm_ReturnsFilteredIngredients()
        {
            // Act
            var result = await _service.GetIngredientsAsync(pageNumber: 1, pageSize: 10, searchTerm: "Sugar");

            // Assert
            Assert.Single(result.Ingredients);
            Assert.Equal("Sugar", result.Ingredients.First().IngredientName);
        }

        [Fact]
        public async Task CreateIngredientAsync_CreatesNewIngredient()
        {
            // Arrange
            var newIngredient = new Ingredient
            {
                IngredientName = "Butter",
                IngredientType = "Dairy"
            };

            // Act
            var result = await _service.CreateIngredientAsync(newIngredient);

            // Assert
            Assert.True(result);
            var saved = await _context.Ingredients.FirstOrDefaultAsync(i => i.IngredientName == "Butter");
            Assert.NotNull(saved);
            Assert.Null(saved!.IsApproved);
        }

        [Fact]
        public async Task CreateIngredientAsync_WithDuplicateName_ReturnsFalse()
        {
            // Arrange
            var duplicate = new Ingredient
            {
                IngredientName = "Sugar",
                IngredientType = "Sweetener"
            };

            // Act
            var result = await _service.CreateIngredientAsync(duplicate);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateIngredientAsync_UpdatesExistingIngredient()
        {
            // Arrange
            var ingredient = new Ingredient
            {
                IngredientID = 1,
                IngredientName = "Brown Sugar",
                IngredientType = "Sweetener",
                IngredientDetails = "Dark brown"
            };

            // Act
            var result = await _service.UpdateIngredientAsync(ingredient);

            // Assert
            Assert.True(result);
            var updated = await _context.Ingredients.FindAsync(1);
            Assert.Equal("Brown Sugar", updated!.IngredientName);
            Assert.Null(updated.IsApproved); // Should be reset to null
        }

        [Fact]
        public async Task ApproveIngredientAsync_UpdatesApprovalStatus()
        {
            // Act
            var result = await _service.ApproveIngredientAsync(3, true);

            // Assert
            Assert.True(result);
            var ingredient = await _context.Ingredients.FindAsync(3);
            Assert.True(ingredient!.IsApproved);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
