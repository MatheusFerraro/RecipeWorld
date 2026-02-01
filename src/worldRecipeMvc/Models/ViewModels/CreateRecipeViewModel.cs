using Microsoft.AspNetCore.Mvc.Rendering;

namespace worldRecipeMvc.Models.ViewModels
{
    public class CreateRecipeViewModel
    {
        public Recipe Recipe { get; set; }

        public List<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();

        public bool? IsApproved { get; }

        //dropdown list for categories
        public SelectList? CategoryList { get; set; }

        //dropdown list for ingredients
        public SelectList? IngredientList { get; set; }

    }
}
