using Microsoft.AspNetCore.Identity;
using worldRecipeMvc.Models;
using System.ComponentModel.DataAnnotations;

namespace worldRecipeMvc.Data
{
    public class RecipeWorldUser : IdentityUser
    {
        
        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

   
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        
        public bool IsSuspended { get; set; } = false;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAtUtc { get; set; }

    
        public string FullName =>
            !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : $"{FirstName} {LastName}".Trim();
        public virtual ICollection<Recipe>? Recipes { get; set; } = new List<Recipe>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }

    
}
