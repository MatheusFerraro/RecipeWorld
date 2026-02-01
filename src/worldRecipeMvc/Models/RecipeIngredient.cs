using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace worldRecipeMvc.Models
{
    public class RecipeIngredient
    {
        [Key]
        
        public int? RecipeID { get; set; }
        public virtual Recipe? Recipe { get; set; }

        [Key]
       
        public int? IngredientID { get; set; }
        public virtual Ingredient? Ingredient { get; set; }

        [Range(0, 1000, ErrorMessage = "Amount must be a positive number")]
        public double? Amount { get; set; } //For exampl: "1.5" or "2"

        [StringLength(50)]
        public string? Unit { get; set; } //example: "cup" or "grams"
    }
}
