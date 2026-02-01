using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using worldRecipeMvc.Data;

namespace worldRecipeMvc.Models
{
    public class Ingredient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? IngredientID { get; set; }

        [Required]
        [Display(Name = "Ingredient Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2-50 characters")]
        public string? IngredientName { get; set; }

        [Required]
        [Display(Name = "Ingredient Type")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Type must be between 2-50 characters")]
        public string? IngredientType { get; set; }

        [Display(Name = "Ingredient Details")]
        [StringLength(1000, ErrorMessage = "Details must be between under 1000 characters")]
        public string? IngredientDetails { get; set; }

        [Display(Name = "Status")]
        public bool? IsApproved { get; set; } = null!;

        [Display(Name = "Owner")]
        public string? OwnerID { get; set; }

        [ForeignKey("OwnerID")]
        public virtual RecipeWorldUser? Owner { get; set; }

        //find all recipes that use this ingredient
        public virtual ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }
}
