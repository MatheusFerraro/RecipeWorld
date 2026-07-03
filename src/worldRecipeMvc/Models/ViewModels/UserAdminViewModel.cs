namespace worldRecipeMvc.Models.ViewModels
{
    /// <summary>Row in the admin user-management hub.</summary>
    public class UserAdminListItem
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastLoginAtUtc { get; set; }
        public bool IsLockedOut { get; set; }
        public int RecipeCount { get; set; }
    }

    public class UserAdminIndexViewModel
    {
        public PagedResult<UserAdminListItem> Users { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string CurrentUserId { get; set; } = string.Empty;
    }
}
