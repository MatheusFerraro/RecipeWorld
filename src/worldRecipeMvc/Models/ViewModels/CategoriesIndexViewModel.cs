namespace worldRecipeMvc.Models.ViewModels
{
    public class CategoriesIndexViewModel
    {
        public PagedResult<Category> Categories { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? SortOrder { get; set; }
    }
}
