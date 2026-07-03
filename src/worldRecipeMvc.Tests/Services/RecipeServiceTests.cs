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
    public class RecipeServiceTests : IDisposable
    {
        private const string OwnerId = "owner-1";
        private const string OtherUserId = "other-user";

        private readonly ApplicationDbContext _context;
        private readonly RecipeService _service;

        public RecipeServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new RecipeService(_context, new MemoryCache(new MemoryCacheOptions()), Mock.Of<ILogger<RecipeService>>());

            SeedTestData();
        }

        private void SeedTestData()
        {
            var category = new Category { CategoryID = 1, CategoryName = "Desserts", IsApproved = true, OwnerID = OwnerId };
            var pendingIngredient = new Ingredient { IngredientID = 1, IngredientName = "Sugar", IngredientType = "Sweetener", IsApproved = null, OwnerID = OwnerId };
            _context.Categories.Add(category);
            _context.Ingredients.Add(pendingIngredient);

            _context.Recipes.AddRange(
                new Recipe { RecipeID = 1, RecipeName = "Public Cake", Instructions = "Bake", Status = RecipeStatus.Public, OwnerID = OwnerId, CategoryID = 1 },
                new Recipe { RecipeID = 2, RecipeName = "Secret Draft", Instructions = "Shh", Status = RecipeStatus.Draft, OwnerID = OwnerId, CategoryID = 1 },
                new Recipe { RecipeID = 3, RecipeName = "Private Pie", Instructions = "Hidden", Status = RecipeStatus.Private, OwnerID = OwnerId, CategoryID = 1 });

            _context.SaveChanges();
        }

        // ---- Listing & visibility ------------------------------------------

        [Fact]
        public async Task GetRecipesAsync_Anonymous_SeesOnlyPublicRecipes()
        {
            var result = await _service.GetRecipesAsync(1, 10, null, null, null, userId: null, isAdmin: false);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Items);
            Assert.Equal("Public Cake", result.Value.Items[0].RecipeName);
        }

        [Fact]
        public async Task GetRecipesAsync_Owner_SeesOwnDraftsAndPublic()
        {
            var result = await _service.GetRecipesAsync(1, 10, null, null, null, OwnerId, isAdmin: false);

            Assert.Equal(3, result.Value.TotalCount);
        }

        [Fact]
        public async Task GetRecipesAsync_OtherUser_DoesNotSeeForeignDrafts()
        {
            var result = await _service.GetRecipesAsync(1, 10, null, null, null, OtherUserId, isAdmin: false);

            Assert.Single(result.Value.Items);
        }

        [Fact]
        public async Task GetRecipesAsync_Admin_SeesEverything()
        {
            var result = await _service.GetRecipesAsync(1, 10, null, null, null, "admin-user", isAdmin: true);

            Assert.Equal(3, result.Value.TotalCount);
        }

        [Fact]
        public async Task GetRecipesAsync_SearchTerm_FiltersByName()
        {
            var result = await _service.GetRecipesAsync(1, 10, "Cake", null, null, OwnerId, isAdmin: false);

            Assert.Single(result.Value.Items);
            Assert.Equal("Public Cake", result.Value.Items[0].RecipeName);
        }

        [Fact]
        public async Task GetPublicRecipesAsync_ReturnsOnlyPublicAsDtos()
        {
            var result = await _service.GetPublicRecipesAsync(1, 10, null);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Items);
            Assert.Equal("Public Cake", result.Value.Items[0].RecipeName);
            Assert.Equal("Desserts", result.Value.Items[0].CategoryName);
        }

        [Fact]
        public async Task GetPublicRecipeAsync_DraftRecipe_ReturnsNotFound()
        {
            var result = await _service.GetPublicRecipeAsync(2);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<NotFoundError>());
        }

        [Fact]
        public async Task GetRecipeForViewingAsync_DraftByStranger_ReturnsForbidden()
        {
            var result = await _service.GetRecipeForViewingAsync(2, OtherUserId, isAdmin: false);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ForbiddenError>());
        }

        [Fact]
        public async Task GetRecipeForViewingAsync_DraftByOwner_Succeeds()
        {
            var result = await _service.GetRecipeForViewingAsync(2, OwnerId, isAdmin: false);

            Assert.True(result.IsSuccess);
        }

        // ---- Create ----------------------------------------------------------

        [Fact]
        public async Task CreateRecipeAsync_SetsDraftStatusOwnerAndDefaultImage()
        {
            var recipe = new Recipe { RecipeName = "New Recipe", Instructions = "Cook" };

            var result = await _service.CreateRecipeAsync(recipe, Enumerable.Empty<RecipeIngredient>(), OtherUserId);

            Assert.True(result.IsSuccess);
            Assert.Equal(RecipeStatus.Draft, result.Value.Status);
            Assert.Equal(OtherUserId, result.Value.OwnerID);
            Assert.Equal(ImageStorageService.DefaultImageUrl, result.Value.ImageUrl);
        }

        [Fact]
        public async Task CreateRecipeAsync_DuplicateName_ReturnsConflict()
        {
            var recipe = new Recipe { RecipeName = "public cake", Instructions = "Copy" };

            var result = await _service.CreateRecipeAsync(recipe, Enumerable.Empty<RecipeIngredient>(), OtherUserId);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ConflictError>());
        }

        [Fact]
        public async Task CreateRecipeAsync_DeduplicatesIngredientsAndSkipsInvalid()
        {
            var recipe = new Recipe { RecipeName = "Dedup Test", Instructions = "Mix" };
            var ingredients = new[]
            {
                new RecipeIngredient { IngredientID = 1, Amount = 2, Unit = "cups" },
                new RecipeIngredient { IngredientID = 1, Amount = 5, Unit = "cups" },   // duplicate
                new RecipeIngredient { IngredientID = 1, Amount = 0, Unit = "cups" },   // zero amount
                new RecipeIngredient { IngredientID = null, Amount = 1, Unit = "tsp" }  // no ingredient
            };

            var result = await _service.CreateRecipeAsync(recipe, ingredients, OtherUserId);

            Assert.True(result.IsSuccess);
            var saved = _context.RecipeIngredients.Where(ri => ri.RecipeID == result.Value.RecipeID).ToList();
            Assert.Single(saved);
            Assert.Equal(2, saved[0].Amount);
        }

        // ---- Update ----------------------------------------------------------

        [Fact]
        public async Task UpdateRecipeAsync_NonOwner_ReturnsForbidden()
        {
            var input = new Recipe { RecipeID = 1, RecipeName = "Stolen", Instructions = "x" };

            var result = await _service.UpdateRecipeAsync(1, input, null, OtherUserId, isAdmin: false);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ForbiddenError>());
        }

        [Fact]
        public async Task UpdateRecipeAsync_DuplicateName_ReturnsConflict()
        {
            var input = new Recipe { RecipeID = 2, RecipeName = "Public Cake", Instructions = "x" };

            var result = await _service.UpdateRecipeAsync(2, input, null, OwnerId, isAdmin: false);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ConflictError>());
        }

        [Fact]
        public async Task UpdateRecipeAsync_OwnerWithPendingIngredient_CannotChangeStatus()
        {
            // Recipe 2 (Draft) uses ingredient 1, which is still pending approval
            _context.RecipeIngredients.Add(new RecipeIngredient { RecipeID = 2, IngredientID = 1, Amount = 1, Unit = "cup" });
            await _context.SaveChangesAsync();

            var input = new Recipe { RecipeID = 2, RecipeName = "Secret Draft", Instructions = "Shh", Status = RecipeStatus.Public };

            var result = await _service.UpdateRecipeAsync(2, input, null, OwnerId, isAdmin: false);

            Assert.True(result.IsSuccess);
            var recipe = await _context.Recipes.FindAsync(2);
            Assert.Equal(RecipeStatus.Draft, recipe!.Status); // status change silently ignored
        }

        [Fact]
        public async Task UpdateRecipeAsync_Admin_CanChangeStatus()
        {
            var input = new Recipe { RecipeID = 2, RecipeName = "Secret Draft", Instructions = "Shh", Status = RecipeStatus.Public };

            var result = await _service.UpdateRecipeAsync(2, input, null, "admin-user", isAdmin: true);

            Assert.True(result.IsSuccess);
            var recipe = await _context.Recipes.FindAsync(2);
            Assert.Equal(RecipeStatus.Public, recipe!.Status);
        }

        // ---- Delete & status -------------------------------------------------

        [Fact]
        public async Task DeleteRecipeAsync_MissingRecipe_ReturnsNotFound()
        {
            var result = await _service.DeleteRecipeAsync(999, OwnerId, isAdmin: false);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<NotFoundError>());
        }

        [Fact]
        public async Task DeleteRecipeAsync_NonOwner_ReturnsForbidden()
        {
            var result = await _service.DeleteRecipeAsync(1, OtherUserId, isAdmin: false);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ForbiddenError>());
        }

        [Fact]
        public async Task DeleteRecipeAsync_Owner_Succeeds()
        {
            var result = await _service.DeleteRecipeAsync(1, OwnerId, isAdmin: false);

            Assert.True(result.IsSuccess);
            Assert.Null(await _context.Recipes.FindAsync(1));
        }

        [Fact]
        public async Task ChangeStatusAsync_InvalidStatus_ReturnsValidationError()
        {
            var result = await _service.ChangeStatusAsync(1, "Published", OwnerId, isAdmin: false);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ValidationError>());
        }

        [Fact]
        public async Task ChangeStatusAsync_Owner_UpdatesStatus()
        {
            var result = await _service.ChangeStatusAsync(2, RecipeStatus.Unlisted, OwnerId, isAdmin: false);

            Assert.True(result.IsSuccess);
            var recipe = await _context.Recipes.FindAsync(2);
            Assert.Equal(RecipeStatus.Unlisted, recipe!.Status);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
