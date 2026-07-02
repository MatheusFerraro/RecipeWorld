namespace worldRecipeMvc.Services
{
    /// <summary>Central registry of IMemoryCache keys so eviction stays in sync.</summary>
    public static class CacheKeys
    {
        public const string AllCategories = "categories:all";
        public const string ApprovedCategories = "categories:approved";
        public const string AllIngredients = "ingredients:all";
        public const string HomeTrending = "home:trending";

        public static readonly TimeSpan DropdownTtl = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan TrendingTtl = TimeSpan.FromMinutes(5);
    }
}
