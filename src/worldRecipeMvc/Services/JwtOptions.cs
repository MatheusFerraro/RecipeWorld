namespace worldRecipeMvc.Services
{
    /// <summary>JWT settings bound from the "Jwt" configuration section.</summary>
    public class JwtOptions
    {
        public const string SectionName = "Jwt";
        public const int MinKeyLength = 32;

        public string Issuer { get; set; } = "RecipeWorld";
        public string Audience { get; set; } = "RecipeWorldApi";
        public int ExpiryMinutes { get; set; } = 60;

        /// <summary>HS256 signing key; must be at least 32 characters.</summary>
        public string Key { get; set; } = string.Empty;
    }
}
