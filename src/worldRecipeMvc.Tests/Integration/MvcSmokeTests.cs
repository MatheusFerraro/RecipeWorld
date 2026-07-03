using System.Net;
using Xunit;

namespace worldRecipeMvc.Tests.Integration
{
    public class MvcSmokeTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public MvcSmokeTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/Recipes")]
        [InlineData("/Categories")]
        [InlineData("/Ingredients")]
        [InlineData("/swagger/v1/swagger.json")]
        public async Task PublicPages_ReturnSuccess(string url)
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Health_ReturnsHealthy()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/health");

            response.EnsureSuccessStatusCode();
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Home_ShowsTrendingSections()
        {
            var client = _factory.CreateClient();

            var html = await client.GetStringAsync("/");

            Assert.Contains("Top Rated Recipes", html);
            Assert.Contains("Most Favorited", html);
        }

        [Fact]
        public async Task Favorites_RequiresLogin()
        {
            var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.GetAsync("/Favorites");

            // Cookie auth redirects anonymous users to the login page
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Identity/Account/Login", response.Headers.Location!.ToString());
        }

        [Fact]
        public async Task ApiError_ReturnsProblemDetailsNotHtml()
        {
            var client = _factory.CreateClient();

            // Bad route parameter type still resolves; use a guaranteed-404 API path
            var response = await client.GetAsync("/api/RecipesApi/99999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains("json", response.Content.Headers.ContentType?.MediaType);
        }
    }
}
