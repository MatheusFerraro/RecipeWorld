using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using worldRecipeMvc.Services;
using worldRecipeMvc.Services.Errors;
using Xunit;

namespace worldRecipeMvc.Tests.Services
{
    public class FavoriteServiceTests : IDisposable
    {
        private const string UserA = "user-a";
        private const string UserB = "user-b";

        private readonly ApplicationDbContext _context;
        private readonly FavoriteService _service;

        public FavoriteServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new FavoriteService(_context, Mock.Of<ILogger<FavoriteService>>());

            _context.Recipes.AddRange(
                new Recipe { RecipeID = 1, RecipeName = "Cake", Instructions = "Bake", Status = RecipeStatus.Public },
                new Recipe { RecipeID = 2, RecipeName = "Pie", Instructions = "Bake", Status = RecipeStatus.Public });
            _context.SaveChanges();
        }

        [Fact]
        public async Task ToggleAsync_FirstCall_AddsFavorite()
        {
            var result = await _service.ToggleAsync(1, UserA);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
            Assert.Equal(1, await _context.Favorites.CountAsync());
        }

        [Fact]
        public async Task ToggleAsync_SecondCall_RemovesFavorite()
        {
            await _service.ToggleAsync(1, UserA);
            var result = await _service.ToggleAsync(1, UserA);

            Assert.True(result.IsSuccess);
            Assert.False(result.Value);
            Assert.Equal(0, await _context.Favorites.CountAsync());
        }

        [Fact]
        public async Task ToggleAsync_MissingRecipe_ReturnsNotFound()
        {
            var result = await _service.ToggleAsync(999, UserA);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<NotFoundError>());
        }

        [Fact]
        public async Task GetInfoAsync_ReturnsCountAndUserState()
        {
            await _service.ToggleAsync(1, UserA);
            await _service.ToggleAsync(1, UserB);

            var infoForA = await _service.GetInfoAsync(1, UserA);
            var infoForAnonymous = await _service.GetInfoAsync(1, null);

            Assert.Equal(2, infoForA.Count);
            Assert.True(infoForA.IsFavoritedByCurrentUser);
            Assert.Equal(2, infoForAnonymous.Count);
            Assert.False(infoForAnonymous.IsFavoritedByCurrentUser);
        }

        [Fact]
        public async Task GetMyFavoritesAsync_ReturnsOnlyOwnFavorites()
        {
            await _service.ToggleAsync(1, UserA);
            await _service.ToggleAsync(2, UserA);
            await _service.ToggleAsync(1, UserB);

            var result = await _service.GetMyFavoritesAsync(UserA, 1, 10);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.TotalCount);
            Assert.All(result.Value.Items, r => Assert.True(r.IsFavorited));
        }

        [Fact]
        public async Task GetMyFavoritesAsync_Paginates()
        {
            await _service.ToggleAsync(1, UserA);
            await _service.ToggleAsync(2, UserA);

            var result = await _service.GetMyFavoritesAsync(UserA, pageNumber: 1, pageSize: 1);

            Assert.Single(result.Value.Items);
            Assert.Equal(2, result.Value.TotalCount);
            Assert.True(result.Value.HasNext);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
