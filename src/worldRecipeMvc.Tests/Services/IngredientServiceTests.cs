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
    public class IngredientServiceTests : IDisposable
    {
        private const string OwnerId = "owner-1";
        private const string OtherUserId = "other-user";

        private readonly ApplicationDbContext _context;
        private readonly IngredientService _service;

        public IngredientServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new IngredientService(_context, new MemoryCache(new MemoryCacheOptions()), Mock.Of<ILogger<IngredientService>>());

            SeedTestData();
        }

        private void SeedTestData()
        {
            var ingredients = new List<Ingredient>
            {
                new Ingredient { IngredientID = 1, IngredientName = "Sugar", IngredientType = "Sweetener", IsApproved = true, OwnerID = OwnerId },
                new Ingredient { IngredientID = 2, IngredientName = "Flour", IngredientType = "Grain", IsApproved = true, OwnerID = OwnerId },
                new Ingredient { IngredientID = 3, IngredientName = "Salt", IngredientType = "Seasoning", IsApproved = null, OwnerID = OwnerId }
            };

            _context.Ingredients.AddRange(ingredients);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetIngredientsAsync_ReturnsPaginatedIngredients()
        {
            var result = await _service.GetIngredientsAsync(pageNumber: 1, pageSize: 2, searchTerm: null);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Items.Count);
            Assert.Equal(3, result.Value.TotalCount);
        }

        [Fact]
        public async Task GetIngredientsAsync_WithSearchTerm_ReturnsFilteredIngredients()
        {
            var result = await _service.GetIngredientsAsync(pageNumber: 1, pageSize: 10, searchTerm: "Sugar");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Items);
            Assert.Equal("Sugar", result.Value.Items.First().IngredientName);
        }

        [Fact]
        public async Task GetIngredientsAsync_ApprovedOnly_ExcludesPending()
        {
            var result = await _service.GetIngredientsAsync(pageNumber: 1, pageSize: 10, searchTerm: null, approvedOnly: true);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.TotalCount);
            Assert.All(result.Value.Items, i => Assert.True(i.IsApproved));
        }

        [Fact]
        public async Task CreateIngredientAsync_CreatesPendingIngredientWithOwner()
        {
            var newIngredient = new Ingredient
            {
                IngredientName = "Butter",
                IngredientType = "Dairy"
            };

            var result = await _service.CreateIngredientAsync(newIngredient, OwnerId);

            Assert.True(result.IsSuccess);
            var saved = await _context.Ingredients.FirstOrDefaultAsync(i => i.IngredientName == "Butter");
            Assert.NotNull(saved);
            Assert.Null(saved!.IsApproved);
            Assert.Equal(OwnerId, saved.OwnerID);
        }

        [Fact]
        public async Task CreateIngredientAsync_WithDuplicateName_ReturnsConflict()
        {
            var duplicate = new Ingredient
            {
                IngredientName = "Sugar",
                IngredientType = "Sweetener"
            };

            var result = await _service.CreateIngredientAsync(duplicate, OwnerId);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ConflictError>());
        }

        [Fact]
        public async Task UpdateIngredientAsync_AsAdmin_UpdatesAndResetsApproval()
        {
            var input = new Ingredient
            {
                IngredientID = 1,
                IngredientName = "Brown Sugar",
                IngredientType = "Sweetener"
            };

            var result = await _service.UpdateIngredientAsync(1, input, OtherUserId, isAdmin: true);

            Assert.True(result.IsSuccess);
            var updated = await _context.Ingredients.FindAsync(1);
            Assert.Equal("Brown Sugar", updated!.IngredientName);
            Assert.Null(updated.IsApproved); // Any edit resets approval to pending
        }

        [Fact]
        public async Task UpdateIngredientAsync_OwnerOfApprovedIngredient_ReturnsForbidden()
        {
            var input = new Ingredient { IngredientID = 1, IngredientName = "Hijack", IngredientType = "x" };

            var result = await _service.UpdateIngredientAsync(1, input, OwnerId, isAdmin: false);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ForbiddenError>());
        }

        [Fact]
        public async Task DeleteIngredientAsync_UsedByRecipe_ReturnsConflict()
        {
            var recipe = new Recipe { RecipeName = "Cake", Instructions = "Bake", Status = RecipeStatus.Public };
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            _context.RecipeIngredients.Add(new RecipeIngredient { RecipeID = recipe.RecipeID, IngredientID = 1, Amount = 1, Unit = "cup" });
            await _context.SaveChangesAsync();

            var result = await _service.DeleteIngredientAsync(1, OtherUserId, isAdmin: true);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ConflictError>());
        }

        [Fact]
        public async Task DeleteIngredientAsync_Unused_AsAdmin_Succeeds()
        {
            var result = await _service.DeleteIngredientAsync(2, OtherUserId, isAdmin: true);

            Assert.True(result.IsSuccess);
            Assert.Null(await _context.Ingredients.FindAsync(2));
        }

        [Fact]
        public async Task SetApprovalAsync_UpdatesApprovalStatus()
        {
            var result = await _service.SetApprovalAsync(3, true);

            Assert.True(result.IsSuccess);
            var ingredient = await _context.Ingredients.FindAsync(3);
            Assert.True(ingredient!.IsApproved);
        }

        [Fact]
        public async Task SetApprovalAsync_MissingIngredient_ReturnsNotFound()
        {
            var result = await _service.SetApprovalAsync(999, true);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<NotFoundError>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
