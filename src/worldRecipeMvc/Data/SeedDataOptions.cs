namespace worldRecipeMvc.Data
{
    /// <summary>
    /// Seed account settings bound from the "SeedData" configuration section.
    /// Passwords are intentionally not defaulted: when a password is missing,
    /// the corresponding account is skipped instead of being created with a
    /// well-known credential.
    /// </summary>
    public class SeedDataOptions
    {
        public const string SectionName = "SeedData";

        public bool Enabled { get; set; } = true;
        public string DemoEmail { get; set; } = "demo@recipeworld.com";
        public string? DemoPassword { get; set; }
        public string AdminEmail { get; set; } = "admin@recipeworld.com";
        public string? AdminPassword { get; set; }
    }
}
