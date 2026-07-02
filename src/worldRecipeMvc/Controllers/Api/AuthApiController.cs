using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using worldRecipeMvc.Data;
using worldRecipeMvc.DTOs;
using worldRecipeMvc.Services;

namespace worldRecipeMvc.Controllers.Api
{
    [Route("api/auth")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<RecipeWorldUser> _userManager;
        private readonly SignInManager<RecipeWorldUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(
            UserManager<RecipeWorldUser> userManager,
            SignInManager<RecipeWorldUser> signInManager,
            ITokenService tokenService,
            ILogger<AuthApiController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        /// <summary>Exchange email + password for a JWT Bearer token.</summary>
        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult> Login([FromBody] LoginRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email!);
            if (user == null)
            {
                return Unauthorized(new ProblemDetails { Status = 401, Title = "Invalid credentials" });
            }

            // Mirrors the cookie login flow: counts lockout attempts and
            // respects the confirmed-account requirement.
            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);
            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Failed API login attempt for {Email} ({Reason})", request.Email,
                    signInResult.IsLockedOut ? "locked out" : signInResult.IsNotAllowed ? "not allowed" : "bad password");

                var title = signInResult.IsLockedOut
                    ? "Account locked out. Try again later."
                    : signInResult.IsNotAllowed
                        ? "Account not confirmed."
                        : "Invalid credentials";
                return Unauthorized(new ProblemDetails { Status = 401, Title = title });
            }

            // Track activity for the admin hub
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var (token, expiresAt) = await _tokenService.CreateTokenAsync(user);

            _logger.LogInformation("API token issued for user {UserId}", user.Id);
            return Ok(new TokenResponseDto { Token = token, ExpiresAtUtc = expiresAt });
        }
    }
}
