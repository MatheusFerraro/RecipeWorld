using Microsoft.AspNetCore.Mvc.Rendering;

namespace worldRecipeMvc.Models.ViewModels
{
    public class EditRecipeViewModel
    {
        public Recipe Recipe { get; set; }

        public List<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();

        //dropdown list for categories
        public SelectList? CategoryList { get; set; }

        //dropdown list for ingredients
        public SelectList? IngredientList { get; set; }

        //dropdown list for recipe status (Edit page only)
        public SelectList? StatusList { get; set; }

    }
}
