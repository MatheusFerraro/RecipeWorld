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
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Rating> Ratings { get; set; }

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

            // list/API queries filter by status constantly
            builder.Entity<Recipe>()
                .HasIndex(r => r.Status);

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

            // Favorites: composite key, cascade with the recipe. The user side is
            // Restrict — cascading both FKs would create multiple cascade paths
            // on SQL Server (User -> Recipe is already SetNull).
            builder.Entity<Favorite>(favorite =>
            {
                favorite.HasKey(f => new { f.UserId, f.RecipeID });

                favorite.HasOne(f => f.Recipe)
                    .WithMany(r => r.Favorites)
                    .HasForeignKey(f => f.RecipeID)
                    .OnDelete(DeleteBehavior.Cascade);

                favorite.HasOne(f => f.User)
                    .WithMany(u => u.Favorites)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Ratings: one per user per recipe, cascade with the recipe.
            builder.Entity<Rating>(rating =>
            {
                rating.HasIndex(r => new { r.RecipeID, r.UserId }).IsUnique();

                rating.HasOne(r => r.Recipe)
                    .WithMany(rec => rec.Ratings)
                    .HasForeignKey(r => r.RecipeID)
                    .OnDelete(DeleteBehavior.Cascade);

                rating.HasOne(r => r.User)
                    .WithMany(u => u.Ratings)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
