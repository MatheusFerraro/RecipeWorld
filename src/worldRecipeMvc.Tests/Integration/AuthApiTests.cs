using System.Net;
using System.Net.Http.Json;
using worldRecipeMvc.DTOs;
using Xunit;

namespace worldRecipeMvc.Tests.Integration
{
    public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsToken()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                Email = CustomWebApplicationFactory.DemoEmail,
                Password = CustomWebApplicationFactory.DemoPassword
            });

            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
            Assert.NotNull(token);
            Assert.False(string.IsNullOrWhiteSpace(token!.Token));
            Assert.True(token.ExpiresAtUtc > DateTime.UtcNow);
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                Email = CustomWebApplicationFactory.DemoEmail,
                Password = "definitely-wrong-password"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithUnknownEmail_Returns401()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                Email = "nobody@recipeworld.com",
                Password = "Whatever123!"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_MalformedRequest_Returns400()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = "not-an-email" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    public class AuthRateLimitTests : IDisposable
    {
        private readonly RateLimitedWebApplicationFactory _factory = new();

        [Fact]
        public async Task Login_ExceedingAuthRateLimit_Returns429()
        {
            var client = _factory.CreateClient();
            var request = new LoginRequestDto { Email = "nobody@recipeworld.com", Password = "Whatever123!" };

            for (int i = 0; i < 3; i++)
            {
                var earlier = await client.PostAsJsonAsync("/api/auth/login", request);
                Assert.Equal(HttpStatusCode.Unauthorized, earlier.StatusCode);
            }

            var limited = await client.PostAsJsonAsync("/api/auth/login", request);
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        }

        public void Dispose() => _factory.Dispose();
    }
}
