using worldRecipeMvc.Data;

namespace worldRecipeMvc.Services
{
    public interface ITokenService
    {
        /// <summary>Issues a signed JWT for the user including role claims.</summary>
        Task<(string Token, DateTime ExpiresAtUtc)> CreateTokenAsync(RecipeWorldUser user);
    }
}
