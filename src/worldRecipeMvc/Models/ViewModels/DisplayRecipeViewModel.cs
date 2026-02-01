using Microsoft.AspNetCore.Identity;
using worldRecipeMvc.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace worldRecipeMvc.Models.ViewModels
{
    public class DisplayRecipeViewModel
    {
        public int? RecipeID { get; set; }

        [Display(Name = "Category")]
        public int? CategoryID { get; set; }
        public virtual Category? Category { get; set; }

        [Display(Name = "Recipe Name")]
        public string? RecipeName { get; set; }

        [Display(Name = "Prep Time")]
        public string? PrepTime { get; set; }

        [Display(Name = "Cook Time")]
        public string? CookTime { get; set; }

        public string? Tips { get; set; }

        [Display(Name = "Number of Servings")]
        public double? NumberOfServings { get; set; }

        public string? Status { get; set; }

        //Store the user ID of the recipe owner
        public string? OwnerID { get; set; }

        //link to the user object
        public virtual RecipeWorldUser? Owner { get; set; }

        public string? Instructions { get; set; }
        [Display(Name = "Image")]
        public string? ImageUrl { get; set; }
        public int? Temperature { get; set; }

        public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }
}
