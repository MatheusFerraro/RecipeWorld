namespace worldRecipeMvc.Models
{
    // Simple string-based recipe status constants (no DB change)
    public static class RecipeStatus
    {
        public const string Draft = "Draft";
        public const string Private = "Private";
        public const string Public = "Public";
        public const string Unlisted = "Unlisted";

        public static readonly string[] All = { Draft, Private, Public, Unlisted };
    }
}
