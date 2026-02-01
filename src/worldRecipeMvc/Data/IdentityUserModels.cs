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

    
        public string FullName =>
            !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : $"{FirstName} {LastName}".Trim();
        public virtual ICollection<Recipe>? Recipes { get; set; } = new List<Recipe>();
    }

    
}
