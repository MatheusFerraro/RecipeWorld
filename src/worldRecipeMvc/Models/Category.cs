using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using worldRecipeMvc.Data;

namespace worldRecipeMvc.Models
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? CategoryID { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2-50 characters")]
        public string? CategoryName { get; set; }

        [Display(Name = "Category Description")]
        [StringLength(1000, ErrorMessage = "Description must be between under 1000 characters")]
        public string? CategoryDescription { get; set; }

        [Display(Name = "Status")]
        public bool? IsApproved { get; set; } = null!;

        [Display(Name = "Owner")]
        public string? OwnerID { get; set; }

        [ForeignKey("OwnerID")]
        public virtual RecipeWorldUser? Owner { get; set; }

        public virtual ICollection<Recipe>? Recipes { get; set; }
    }
}
