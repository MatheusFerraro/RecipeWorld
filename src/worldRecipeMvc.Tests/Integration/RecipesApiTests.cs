using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using worldRecipeMvc.DTOs;
using Xunit;

namespace worldRecipeMvc.Tests.Integration
{
    public class RecipesApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public RecipesApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Email = email, Password = password });
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Token);
            return client;
        }

        [Fact]
        public async Task GetRecipes_ReturnsSeededPublicRecipes()
        {
            // Regression lock for the old Status == "Published" bug that made
            // this endpoint always return an empty list.
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/RecipesApi");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("items");
            Assert.True(items.GetArrayLength() > 0, "Seeded public recipes should be returned");
            Assert.All(items.EnumerateArray(), item =>
                Assert.Equal("Public", item.GetProperty("status").GetString()));
        }

        [Fact]
        public async Task GetRecipe_MissingId_Returns404ProblemDetails()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/RecipesApi/99999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateRecipe_WithoutToken_Returns401()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/RecipesApi", new CreateRecipeDto
            {
                RecipeName = "Unauthorized Recipe",
                Instructions = "Should not be created"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateRecipe_WithToken_Returns201AndDraftStatus()
        {
            var client = await CreateAuthenticatedClientAsync(
                CustomWebApplicationFactory.DemoEmail, CustomWebApplicationFactory.DemoPassword);

            var response = await client.PostAsJsonAsync("/api/RecipesApi", new CreateRecipeDto
            {
                RecipeName = $"API Created {Guid.NewGuid():N}",
                Instructions = "1. Mix. 2. Cook.",
                PrepTime = 10,
                CookTime = 20
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            var created = JsonSerializer.Deserialize<RecipeDto>(await response.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal("Draft", created!.Status);
        }

        [Fact]
        public async Task CreateRecipe_MissingRequiredFields_Returns400()
        {
            var client = await CreateAuthenticatedClientAsync(
                CustomWebApplicationFactory.DemoEmail, CustomWebApplicationFactory.DemoPassword);

            var response = await client.PostAsJsonAsync("/api/RecipesApi", new CreateRecipeDto
            {
                // RecipeName and Instructions omitted
                PrepTime = 10
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateRecipe_DuplicateName_Returns409()
        {
            var client = await CreateAuthenticatedClientAsync(
                CustomWebApplicationFactory.DemoEmail, CustomWebApplicationFactory.DemoPassword);

            // "Brigadeiro" is part of the seed data
            var response = await client.PostAsJsonAsync("/api/RecipesApi", new CreateRecipeDto
            {
                RecipeName = "Brigadeiro",
                Instructions = "Duplicate"
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task UpdateRecipe_AsNonOwner_Returns403()
        {
            var strangerEmail = $"stranger-{Guid.NewGuid():N}@test.com";
            await _factory.CreateUserAsync(strangerEmail, "Stranger123!");
            var client = await CreateAuthenticatedClientAsync(strangerEmail, "Stranger123!");

            // Recipe 1 is owned by the demo user
            var response = await client.PutAsJsonAsync("/api/RecipesApi/1", new UpdateRecipeDto
            {
                RecipeName = "Hijacked",
                Instructions = "Should be forbidden"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RateRecipe_AsNonOwner_UpdatesSummary()
        {
            var raterEmail = $"rater-{Guid.NewGuid():N}@test.com";
            await _factory.CreateUserAsync(raterEmail, "Rater1234!");
            var client = await CreateAuthenticatedClientAsync(raterEmail, "Rater1234!");

            var response = await client.PostAsJsonAsync("/api/RecipesApi/1/ratings", new CreateRatingDto
            {
                Stars = 5,
                Comment = "Excellent!"
            });

            response.EnsureSuccessStatusCode();
            var summary = JsonSerializer.Deserialize<RatingSummaryDto>(await response.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Equal(5, summary!.YourStars);
            Assert.True(summary.Count >= 1);
        }

        [Fact]
        public async Task ToggleFavorite_TogglesState()
        {
            var favEmail = $"fav-{Guid.NewGuid():N}@test.com";
            await _factory.CreateUserAsync(favEmail, "Favorite1!");
            var client = await CreateAuthenticatedClientAsync(favEmail, "Favorite1!");

            var first = await client.PostAsync("/api/RecipesApi/1/favorite", null);
            first.EnsureSuccessStatusCode();
            var firstStatus = JsonSerializer.Deserialize<FavoriteStatusDto>(await first.Content.ReadAsStringAsync(), JsonOptions);
            Assert.True(firstStatus!.IsFavorited);

            var second = await client.PostAsync("/api/RecipesApi/1/favorite", null);
            second.EnsureSuccessStatusCode();
            var secondStatus = JsonSerializer.Deserialize<FavoriteStatusDto>(await second.Content.ReadAsStringAsync(), JsonOptions);
            Assert.False(secondStatus!.IsFavorited);
        }

        [Fact]
        public async Task GetIngredients_ReturnsOnlyApproved()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/IngredientsApi?pageSize=100");

            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var items = doc.RootElement.GetProperty("items");
            Assert.True(items.GetArrayLength() > 0);
            Assert.All(items.EnumerateArray(), item =>
                Assert.True(item.GetProperty("isApproved").GetBoolean()));
        }

        [Fact]
        public async Task CreateIngredient_CannotOverpostApproval()
        {
            var client = await CreateAuthenticatedClientAsync(
                CustomWebApplicationFactory.DemoEmail, CustomWebApplicationFactory.DemoPassword);

            // Send extra fields a malicious client might add; binder must ignore them
            var payload = new
            {
                ingredientName = $"Overpost {Guid.NewGuid():N}"[..20],
                ingredientType = "Test",
                isApproved = true,
                ownerID = "someone-else"
            };

            var response = await client.PostAsJsonAsync("/api/IngredientsApi", payload);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = JsonSerializer.Deserialize<IngredientDto>(await response.Content.ReadAsStringAsync(), JsonOptions);
            Assert.Null(created!.IsApproved); // still pending despite the payload
        }
    }
}
