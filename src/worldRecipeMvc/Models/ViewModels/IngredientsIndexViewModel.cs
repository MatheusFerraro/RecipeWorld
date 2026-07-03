namespace worldRecipeMvc.Models.ViewModels
{
    public class IngredientsIndexViewModel
    {
        public PagedResult<Ingredient> Ingredients { get; set; } = new();
        public string? SearchTerm { get; set; }
    }
}
