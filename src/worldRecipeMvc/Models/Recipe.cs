using Microsoft.AspNetCore.Identity;
using worldRecipeMvc.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace worldRecipeMvc.Models
{
    public class Recipe
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? RecipeID { get; set; }

        [Display(Name = "Category")]
        public int? CategoryID { get; set; }
        public virtual Category? Category { get; set; }

        [Required]
        [Display(Name = "Recipe Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2-50 characters")]
        public string? RecipeName { get; set; }

        [Display(Name = "Prep Time")]
        [Range(0, 1000, ErrorMessage = "Prep Time must be a positive number")]
        public int? PrepTime { get; set; }

        [Display(Name = "Cook Time")]
        [Range(0, 1000, ErrorMessage = "Cook Time must be a positive number")]
        public int? CookTime { get; set; }

        public string? Tips { get; set; }

        [Display(Name = "Number of Servings")]
        [Range(0, 1000, ErrorMessage = "Number of Servings must be a positive number")]
        public double? NumberOfServings { get; set; }

        public string? Status { get; set; }

        //Store the user ID of the recipe owner
        public string? OwnerID { get; set; }

        //link to the user object
        public virtual RecipeWorldUser? Owner { get; set; }

        [Required]
        public string? Instructions { get; set; }

        [Display(Name = "Image")]
        public string? ImageUrl { get; set; }

        [Range(0, 1000, ErrorMessage = "Temperature must be a positive number")]
        public int? Temperature { get; set; }

        public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    }

}
