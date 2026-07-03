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
    public class RatingServiceTests : IDisposable
    {
        private const string OwnerId = "owner-1";
        private const string ReviewerA = "reviewer-a";
        private const string ReviewerB = "reviewer-b";

        private readonly ApplicationDbContext _context;
        private readonly RatingService _service;

        public RatingServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new RatingService(_context, Mock.Of<ILogger<RatingService>>());

            _context.Users.AddRange(
                new RecipeWorldUser { Id = OwnerId, UserName = "owner@test.com" },
                new RecipeWorldUser { Id = ReviewerA, UserName = "a@test.com" },
                new RecipeWorldUser { Id = ReviewerB, UserName = "b@test.com" });
            _context.Recipes.Add(new Recipe { RecipeID = 1, RecipeName = "Cake", Instructions = "Bake", Status = RecipeStatus.Public, OwnerID = OwnerId });
            _context.SaveChanges();
        }

        [Fact]
        public async Task UpsertAsync_NewRating_Creates()
        {
            var result = await _service.UpsertAsync(1, ReviewerA, 4, "Tasty!");

            Assert.True(result.IsSuccess);
            Assert.Equal(4, result.Value.Stars);
            Assert.Equal(1, await _context.Ratings.CountAsync());
        }

        [Fact]
        public async Task UpsertAsync_ExistingRating_UpdatesInsteadOfDuplicating()
        {
            await _service.UpsertAsync(1, ReviewerA, 4, "Good");
            var result = await _service.UpsertAsync(1, ReviewerA, 2, "Changed my mind");

            Assert.True(result.IsSuccess);
            Assert.Equal(1, await _context.Ratings.CountAsync());
            var rating = await _context.Ratings.SingleAsync();
            Assert.Equal(2, rating.Stars);
            Assert.Equal("Changed my mind", rating.Comment);
            Assert.NotNull(rating.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        public async Task UpsertAsync_StarsOutOfRange_ReturnsValidationError(int stars)
        {
            var result = await _service.UpsertAsync(1, ReviewerA, stars, null);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ValidationError>());
        }

        [Fact]
        public async Task UpsertAsync_OwnerRatesOwnRecipe_ReturnsValidationError()
        {
            var result = await _service.UpsertAsync(1, OwnerId, 5, "I am amazing");

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ValidationError>());
        }

        [Fact]
        public async Task UpsertAsync_MissingRecipe_ReturnsNotFound()
        {
            var result = await _service.UpsertAsync(999, ReviewerA, 4, null);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<NotFoundError>());
        }

        [Fact]
        public async Task GetSummaryAsync_ComputesAverageAndCount()
        {
            await _service.UpsertAsync(1, ReviewerA, 4, null);
            await _service.UpsertAsync(1, ReviewerB, 2, null);

            var summary = await _service.GetSummaryAsync(1, ReviewerA);

            Assert.Equal(3.0, summary.AverageStars);
            Assert.Equal(2, summary.Count);
            Assert.NotNull(summary.CurrentUserRating);
            Assert.Equal(4, summary.CurrentUserRating!.Stars);
        }

        [Fact]
        public async Task GetSummaryAsync_NoRatings_ReturnsEmptySummary()
        {
            var summary = await _service.GetSummaryAsync(1, null);

            Assert.Null(summary.AverageStars);
            Assert.Equal(0, summary.Count);
            Assert.Null(summary.CurrentUserRating);
        }

        [Fact]
        public async Task DeleteAsync_OwnRating_Succeeds()
        {
            await _service.UpsertAsync(1, ReviewerA, 4, null);

            var result = await _service.DeleteAsync(1, ReviewerA, isAdmin: false);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, await _context.Ratings.CountAsync());
        }

        [Fact]
        public async Task GetReviewsAsync_PaginatesNewestFirst()
        {
            await _service.UpsertAsync(1, ReviewerA, 4, "first");
            await _service.UpsertAsync(1, ReviewerB, 5, "second");

            var result = await _service.GetReviewsAsync(1, pageNumber: 1, pageSize: 1);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Items);
            Assert.Equal(2, result.Value.TotalCount);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
