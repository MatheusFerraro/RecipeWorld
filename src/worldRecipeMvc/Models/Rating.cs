using worldRecipeMvc.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace worldRecipeMvc.Models
{
    /// <summary>A 1-5 star review of a recipe; one per user per recipe.</summary>
    public class Rating
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RatingID { get; set; }

        public int RecipeID { get; set; }
        public virtual Recipe? Recipe { get; set; }

        public string UserId { get; set; } = null!;
        public virtual RecipeWorldUser? User { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
        public int Stars { get; set; }

        [StringLength(1000, ErrorMessage = "Review must be under 1000 characters")]
        public string? Comment { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
