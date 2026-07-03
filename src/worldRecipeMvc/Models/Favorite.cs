using worldRecipeMvc.Data;

namespace worldRecipeMvc.Models
{
    /// <summary>A user's bookmark of a recipe. Composite key (UserId, RecipeID).</summary>
    public class Favorite
    {
        public string UserId { get; set; } = null!;
        public virtual RecipeWorldUser? User { get; set; }

        public int RecipeID { get; set; }
        public virtual Recipe? Recipe { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
