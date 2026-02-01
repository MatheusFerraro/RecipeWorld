using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Models;

namespace worldRecipeMvc.Data
{
    public class ApplicationDbContext : IdentityDbContext<RecipeWorldUser>
    {
        public DbSet<Recipe> Recipes { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); 

            // unique recipe names
            builder.Entity<Recipe>()
            .HasIndex(r => r.RecipeName)
            .IsUnique();

            builder.Entity<Category>()
                .HasMany(c => c.Recipes)
                .WithOne(r => r.Category)
                .HasForeignKey(r => r.CategoryID)
                .HasConstraintName("FK_Recipe_CategoryID");

            builder.Entity<RecipeIngredient>()
                .HasKey(ri => new { ri.RecipeID, ri.IngredientID });

            builder.Entity<Recipe>()
                .HasOne(r => r.Owner)
                .WithMany(u => u.Recipes)
                .HasForeignKey(r => r.OwnerID)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Recipe_OwnerID");
        }
    }
}
