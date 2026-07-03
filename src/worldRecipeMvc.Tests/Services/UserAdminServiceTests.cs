using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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
    public class UserAdminServiceTests : IDisposable
    {
        private const string AdminId = "admin-1";
        private const string MemberId = "member-1";

        private readonly ApplicationDbContext _context;
        private readonly UserAdminService _service;
        private readonly UserManager<RecipeWorldUser> _userManager;

        public UserAdminServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var store = new UserStore<RecipeWorldUser>(_context);
            _userManager = new UserManager<RecipeWorldUser>(
                store, null, new PasswordHasher<RecipeWorldUser>(),
                Array.Empty<IUserValidator<RecipeWorldUser>>(),
                Array.Empty<IPasswordValidator<RecipeWorldUser>>(),
                new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null,
                Mock.Of<ILogger<UserManager<RecipeWorldUser>>>());

            _service = new UserAdminService(_context, _userManager, Mock.Of<ILogger<UserAdminService>>());

            _context.Users.AddRange(
                new RecipeWorldUser { Id = AdminId, UserName = "admin@test.com", Email = "admin@test.com", NormalizedEmail = "ADMIN@TEST.COM", CreatedAtUtc = DateTime.UtcNow.AddDays(-10) },
                new RecipeWorldUser { Id = MemberId, UserName = "member@test.com", Email = "member@test.com", NormalizedEmail = "MEMBER@TEST.COM", CreatedAtUtc = DateTime.UtcNow.AddDays(-5) });
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsPagedUsersNewestFirst()
        {
            var result = await _service.GetUsersAsync(1, 10, null);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.TotalCount);
            Assert.Equal(MemberId, result.Value.Items[0].Id); // newer account first
        }

        [Fact]
        public async Task GetUsersAsync_SearchFiltersOnEmail()
        {
            var result = await _service.GetUsersAsync(1, 10, "member");

            Assert.Single(result.Value.Items);
            Assert.Equal("member@test.com", result.Value.Items[0].Email);
        }

        [Fact]
        public async Task LockAsync_LocksTheAccount()
        {
            var result = await _service.LockAsync(MemberId, AdminId);

            Assert.True(result.IsSuccess);
            var user = await _context.Users.FindAsync(MemberId);
            Assert.NotNull(user!.LockoutEnd);
            Assert.True(user.LockoutEnd > DateTimeOffset.UtcNow.AddYears(50));
        }

        [Fact]
        public async Task LockAsync_Self_ReturnsValidationError()
        {
            var result = await _service.LockAsync(AdminId, AdminId);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ValidationError>());
        }

        [Fact]
        public async Task UnlockAsync_ClearsLockout()
        {
            await _service.LockAsync(MemberId, AdminId);

            var result = await _service.UnlockAsync(MemberId);

            Assert.True(result.IsSuccess);
            var user = await _context.Users.FindAsync(MemberId);
            Assert.Null(user!.LockoutEnd);
        }

        [Fact]
        public async Task DeleteAsync_Self_ReturnsValidationError()
        {
            var result = await _service.DeleteAsync(AdminId, AdminId);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<ValidationError>());
        }

        [Fact]
        public async Task DeleteAsync_RemovesUserAndSocialData_KeepsContent()
        {
            var recipe = new Recipe { RecipeID = 1, RecipeName = "Cake", Instructions = "Bake", Status = RecipeStatus.Public, OwnerID = AdminId };
            var category = new Category { CategoryID = 1, CategoryName = "Owned", OwnerID = MemberId };
            _context.Recipes.Add(recipe);
            _context.Categories.Add(category);
            _context.Favorites.Add(new Favorite { UserId = MemberId, RecipeID = 1 });
            _context.Ratings.Add(new Rating { RecipeID = 1, UserId = MemberId, Stars = 4 });
            await _context.SaveChangesAsync();

            var result = await _service.DeleteAsync(MemberId, AdminId);

            Assert.True(result.IsSuccess);
            Assert.Null(await _context.Users.FindAsync(MemberId));
            Assert.Empty(_context.Favorites.Where(f => f.UserId == MemberId));
            Assert.Empty(_context.Ratings.Where(r => r.UserId == MemberId));
            var survivingCategory = await _context.Categories.FindAsync(1);
            Assert.NotNull(survivingCategory);           // content survives
            Assert.Null(survivingCategory!.OwnerID);     // ownership cleared
        }

        [Fact]
        public async Task DeleteAsync_MissingUser_ReturnsNotFound()
        {
            var result = await _service.DeleteAsync("ghost", AdminId);

            Assert.True(result.IsFailed);
            Assert.True(result.HasError<NotFoundError>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _userManager.Dispose();
        }
    }
}
