using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Models;

namespace worldRecipeMvc.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<RecipeWorldUser> userManager)
        {
            var options = serviceProvider.GetRequiredService<IConfiguration>()
                .GetSection(SeedDataOptions.SectionName)
                .Get<SeedDataOptions>() ?? new SeedDataOptions();

            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SeedData));

            if (!options.Enabled)
            {
                logger.LogInformation("Sample data seeding is disabled via configuration");
                return;
            }

            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // Check if database already has data
            if (context.Categories.Any() || context.Recipes.Any())
            {
                return; // Database has been seeded
            }

            // Create demo user
            var demoUser = await CreateDemoUser(userManager, options, logger);

            // Seed Categories
            var categories = new Category[]
            {
                new Category { CategoryName = "Desserts", CategoryDescription = "Sweet treats and desserts from around the world", IsApproved = true, OwnerID = demoUser?.Id },
                new Category { CategoryName = "Main Course", CategoryDescription = "Hearty main dishes and entrees", IsApproved = true, OwnerID = demoUser?.Id },
                new Category { CategoryName = "Appetizers", CategoryDescription = "Starters and small plates", IsApproved = true, OwnerID = demoUser?.Id },
                new Category { CategoryName = "Soups", CategoryDescription = "Hot and cold soups", IsApproved = true, OwnerID = demoUser?.Id },
                new Category { CategoryName = "Salads", CategoryDescription = "Fresh salads and bowls", IsApproved = true, OwnerID = demoUser?.Id },
                new Category { CategoryName = "Breakfast", CategoryDescription = "Morning meals and brunch", IsApproved = true, OwnerID = demoUser?.Id }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            // Seed Ingredients
            var ingredients = new Ingredient[]
            {
                // Dairy
                new Ingredient { IngredientName = "Sweetened Condensed Milk", IngredientType = "Dairy", IngredientDetails = "Canned sweetened condensed milk", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Butter", IngredientType = "Dairy", IngredientDetails = "Unsalted butter", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Heavy Cream", IngredientType = "Dairy", IngredientDetails = "Full fat heavy cream", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Parmesan Cheese", IngredientType = "Dairy", IngredientDetails = "Grated Parmesan cheese", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Mozzarella", IngredientType = "Dairy", IngredientDetails = "Fresh mozzarella cheese", IsApproved = true, OwnerID = demoUser?.Id },
                
                // Proteins
                new Ingredient { IngredientName = "Chicken Breast", IngredientType = "Protein", IngredientDetails = "Boneless, skinless chicken breast", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Ground Beef", IngredientType = "Protein", IngredientDetails = "Lean ground beef", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Eggs", IngredientType = "Protein", IngredientDetails = "Large eggs", IsApproved = true, OwnerID = demoUser?.Id },
                
                // Carbs & Grains
                new Ingredient { IngredientName = "Spaghetti", IngredientType = "Pasta", IngredientDetails = "Dried spaghetti pasta", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Flour", IngredientType = "Grain", IngredientDetails = "All-purpose flour", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Rice", IngredientType = "Grain", IngredientDetails = "Long grain white rice", IsApproved = true, OwnerID = demoUser?.Id },
                
                // Vegetables
                new Ingredient { IngredientName = "Tomatoes", IngredientType = "Vegetable", IngredientDetails = "Fresh ripe tomatoes", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Onion", IngredientType = "Vegetable", IngredientDetails = "Yellow onion", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Garlic", IngredientType = "Vegetable", IngredientDetails = "Fresh garlic cloves", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Bell Pepper", IngredientType = "Vegetable", IngredientDetails = "Red or green bell pepper", IsApproved = true, OwnerID = demoUser?.Id },
                
                // Spices & Seasonings
                new Ingredient { IngredientName = "Cocoa Powder", IngredientType = "Baking", IngredientDetails = "Unsweetened cocoa powder", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Chocolate Sprinkles", IngredientType = "Baking", IngredientDetails = "Chocolate sprinkles for decoration", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Salt", IngredientType = "Seasoning", IngredientDetails = "Table salt or sea salt", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Black Pepper", IngredientType = "Seasoning", IngredientDetails = "Freshly ground black pepper", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Olive Oil", IngredientType = "Oil", IngredientDetails = "Extra virgin olive oil", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Basil", IngredientType = "Herb", IngredientDetails = "Fresh basil leaves", IsApproved = true, OwnerID = demoUser?.Id },
                
                // Baking
                new Ingredient { IngredientName = "Sugar", IngredientType = "Sweetener", IngredientDetails = "Granulated white sugar", IsApproved = true, OwnerID = demoUser?.Id },
                new Ingredient { IngredientName = "Vanilla Extract", IngredientType = "Flavoring", IngredientDetails = "Pure vanilla extract", IsApproved = true, OwnerID = demoUser?.Id }
            };

            await context.Ingredients.AddRangeAsync(ingredients);
            await context.SaveChangesAsync();

            // Seed Recipes
            var recipes = new Recipe[]
            {
                // 1. Brigadeiro (Brazilian Dessert)
                new Recipe
                {
                    RecipeName = "Brigadeiro",
                    CategoryID = categories[0].CategoryID,
                    PrepTime = 5,
                    CookTime = 15,
                    NumberOfServings = 20,
                    Status = RecipeStatus.Public,
                    OwnerID = demoUser?.Id,
                    Temperature = 0,
                    ImageUrl = "https://t3.ftcdn.net/jpg/03/01/55/56/360_F_301555615_PuTf9kLR9GqVKDMs8kVdwyUB4yjOjNIz.jpg",
                    Instructions = @"1. In a medium saucepan, combine the condensed milk, cocoa powder, and butter.
2. Cook over medium heat, stirring constantly with a wooden spoon or spatula.
3. The mixture will start to thicken after about 10-15 minutes. Keep stirring!
4. The brigadeiro is ready when you can see the bottom of the pan when you tilt it, and the mixture slowly slides to one side.
5. Remove from heat and pour into a greased plate or dish.
6. Let it cool to room temperature, then refrigerate for about 30 minutes.
7. Grease your hands with butter, take small portions and roll into balls.
8. Roll each ball in chocolate sprinkles until fully covered.
9. Place in small paper cups and serve!",
                    Tips = "Don't stop stirring or the brigadeiro will burn! The mixture should be thick enough to hold its shape but still creamy. This is a traditional Brazilian treat served at birthday parties!"
                },

                // 2. Spaghetti Carbonara
                new Recipe
                {
                    RecipeName = "Spaghetti Carbonara",
                    CategoryID = categories[1].CategoryID,
                    PrepTime = 10,
                    CookTime = 20,
                    NumberOfServings = 4,
                    Status = RecipeStatus.Public,
                    OwnerID = demoUser?.Id,
                    Temperature = 0,
                    ImageUrl = "https://images.unsplash.com/photo-1612874742237-6526221588e3?w=500",
                    Instructions = @"1. Bring a large pot of salted water to boil and cook spaghetti according to package directions.
2. While pasta cooks, beat eggs and mix with grated Parmesan cheese in a bowl.
3. Add black pepper to the egg mixture.
4. Reserve 1 cup of pasta water before draining.
5. Immediately toss hot pasta with the egg mixture (off heat).
6. Add pasta water gradually to create a creamy sauce.
7. The heat from the pasta will cook the eggs gently.
8. Serve immediately with extra Parmesan and black pepper.",
                    Tips = "Make sure to remove the pan from heat before adding the egg mixture to prevent scrambling. The key is using the residual heat from the pasta to create a creamy sauce."
                },

                // 3. Chicken Tikka Masala
                new Recipe
                {
                    RecipeName = "Chicken Tikka Masala",
                    CategoryID = categories[1].CategoryID,
                    PrepTime = 30,
                    CookTime = 40,
                    NumberOfServings = 6,
                    Status = RecipeStatus.Public,
                    OwnerID = demoUser?.Id,
                    Temperature = 425,
                    ImageUrl = "https://images.unsplash.com/photo-1565557623262-b51c2513a641?w=500",
                    Instructions = @"1. Cut chicken into bite-sized pieces and marinate for at least 30 minutes.
2. Heat oil in a large pan over medium-high heat.
3. Cook chicken pieces until lightly charred and cooked through.
4. Remove chicken and set aside.
5. In the same pan, saut� onions until soft.
6. Add garlic and cook for 1 minute.
7. Add tomatoes and cook until they break down.
8. Add heavy cream and bring to a simmer.
9. Return chicken to the pan and simmer for 10 minutes.
10. Serve hot with rice or naan bread.",
                    Tips = "For best results, marinate the chicken overnight. You can adjust the spice level to your preference. This dish freezes well!"
                },

                // 4. Margherita Pizza
                new Recipe
                {
                    RecipeName = "Margherita Pizza",
                    CategoryID = categories[1].CategoryID,
                    PrepTime = 90,
                    CookTime = 12,
                    NumberOfServings = 4,
                    Status = RecipeStatus.Public,
                    OwnerID = demoUser?.Id,
                    Temperature = 475,
                    ImageUrl = "https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=500",
                    Instructions = @"1. Prepare pizza dough and let it rise for 1 hour.
2. Preheat oven to 475�F (245�C).
3. Roll out dough into a 12-inch circle.
4. Brush with olive oil.
5. Spread crushed tomatoes evenly over the dough.
6. Add torn mozzarella pieces.
7. Season with salt and pepper.
8. Bake for 10-12 minutes until crust is golden and cheese is bubbly.
9. Top with fresh basil leaves.
10. Drizzle with olive oil and serve immediately.",
                    Tips = "Use a pizza stone if you have one for a crispier crust. Fresh mozzarella makes a huge difference in taste!"
                },

                // 5. Classic Pancakes
                new Recipe
                {
                    RecipeName = "Classic Pancakes",
                    CategoryID = categories[5].CategoryID,
                    PrepTime = 10,
                    CookTime = 15,
                    NumberOfServings = 4,
                    Status = RecipeStatus.Public,
                    OwnerID = demoUser?.Id,
                    Temperature = 0,
                    ImageUrl = "/WebsiteImages/Protein-Pancakes.jpg",
                    Instructions = @"1. Mix flour, sugar, salt in a large bowl.
2. In another bowl, whisk eggs, milk, and melted butter.
3. Pour wet ingredients into dry ingredients and mix until just combined (lumps are okay!).
4. Heat a griddle or pan over medium heat and lightly grease.
5. Pour 1/4 cup batter for each pancake.
6. Cook until bubbles form on surface, about 2-3 minutes.
7. Flip and cook another 1-2 minutes until golden.
8. Serve hot with maple syrup and butter.",
                    Tips = "Don't overmix the batter - a few lumps are fine and will result in fluffier pancakes. Let the batter rest for 5 minutes before cooking for even better results."
                },

                // 6. Caesar Salad
                new Recipe
                {
                    RecipeName = "Caesar Salad",
                    CategoryID = categories[4].CategoryID,
                    PrepTime = 15,
                    CookTime = 0,
                    NumberOfServings = 4,
                    Status = RecipeStatus.Public,
                    OwnerID = demoUser?.Id,
                    Temperature = 0,
                    ImageUrl = "https://images.unsplash.com/photo-1550304943-4f24f54ddde9?w=500",
                    Instructions = @"1. Wash and dry romaine lettuce, then tear into bite-sized pieces.
2. In a bowl, mash garlic with salt to form a paste.
3. Add egg yolks and whisk vigorously.
4. Slowly drizzle in olive oil while whisking to create an emulsion.
5. Add Parmesan cheese and black pepper.
6. Toss lettuce with dressing until well coated.
7. Top with extra Parmesan and croutons.
8. Serve immediately.",
                    Tips = "For a safer option, use pasteurized eggs. Add grilled chicken to make it a complete meal!"
                }
            };

            await context.Recipes.AddRangeAsync(recipes);
            await context.SaveChangesAsync();

            // Seed Recipe Ingredients (connecting recipes with ingredients)
            var recipeIngredients = new List<RecipeIngredient>();

            // Brigadeiro ingredients
            recipeIngredients.AddRange(new[]
            {
                new RecipeIngredient { RecipeID = recipes[0].RecipeID, IngredientID = ingredients[0].IngredientID, Amount = 1, Unit = "can (14 oz)" },
                new RecipeIngredient { RecipeID = recipes[0].RecipeID, IngredientID = ingredients[15].IngredientID, Amount = 3, Unit = "tablespoons" },
                new RecipeIngredient { RecipeID = recipes[0].RecipeID, IngredientID = ingredients[1].IngredientID, Amount = 1, Unit = "tablespoon" },
                new RecipeIngredient { RecipeID = recipes[0].RecipeID, IngredientID = ingredients[16].IngredientID, Amount = 1, Unit = "cup" }
            });

            // Spaghetti Carbonara ingredients
            recipeIngredients.AddRange(new[]
            {
                new RecipeIngredient { RecipeID = recipes[1].RecipeID, IngredientID = ingredients[8].IngredientID, Amount = 400, Unit = "grams" },
                new RecipeIngredient { RecipeID = recipes[1].RecipeID, IngredientID = ingredients[7].IngredientID, Amount = 4, Unit = "large" },
                new RecipeIngredient { RecipeID = recipes[1].RecipeID, IngredientID = ingredients[3].IngredientID, Amount = 1, Unit = "cup" },
                new RecipeIngredient { RecipeID = recipes[1].RecipeID, IngredientID = ingredients[18].IngredientID, Amount = 2, Unit = "teaspoons" }
            });

            // Chicken Tikka Masala ingredients
            recipeIngredients.AddRange(new[]
            {
                new RecipeIngredient { RecipeID = recipes[2].RecipeID, IngredientID = ingredients[5].IngredientID, Amount = 1.5, Unit = "lbs" },
                new RecipeIngredient { RecipeID = recipes[2].RecipeID, IngredientID = ingredients[2].IngredientID, Amount = 1, Unit = "cup" },
                new RecipeIngredient { RecipeID = recipes[2].RecipeID, IngredientID = ingredients[11].IngredientID, Amount = 2, Unit = "large" },
                new RecipeIngredient { RecipeID = recipes[2].RecipeID, IngredientID = ingredients[12].IngredientID, Amount = 1, Unit = "medium" },
                new RecipeIngredient { RecipeID = recipes[2].RecipeID, IngredientID = ingredients[13].IngredientID, Amount = 4, Unit = "cloves" }
            });

            // Margherita Pizza ingredients
            recipeIngredients.AddRange(new[]
            {
                new RecipeIngredient { RecipeID = recipes[3].RecipeID, IngredientID = ingredients[9].IngredientID, Amount = 2, Unit = "cups" },
                new RecipeIngredient { RecipeID = recipes[3].RecipeID, IngredientID = ingredients[4].IngredientID, Amount = 8, Unit = "oz" },
                new RecipeIngredient { RecipeID = recipes[3].RecipeID, IngredientID = ingredients[11].IngredientID, Amount = 3, Unit = "medium" },
                new RecipeIngredient { RecipeID = recipes[3].RecipeID, IngredientID = ingredients[19].IngredientID, Amount = 3, Unit = "tablespoons" },
                new RecipeIngredient { RecipeID = recipes[3].RecipeID, IngredientID = ingredients[20].IngredientID, Amount = 10, Unit = "leaves" }
            });

            // Classic Pancakes ingredients
            recipeIngredients.AddRange(new[]
            {
                new RecipeIngredient { RecipeID = recipes[4].RecipeID, IngredientID = ingredients[9].IngredientID, Amount = 1.5, Unit = "cups" },
                new RecipeIngredient { RecipeID = recipes[4].RecipeID, IngredientID = ingredients[7].IngredientID, Amount = 2, Unit = "large" },
                new RecipeIngredient { RecipeID = recipes[4].RecipeID, IngredientID = ingredients[1].IngredientID, Amount = 3, Unit = "tablespoons" },
                new RecipeIngredient { RecipeID = recipes[4].RecipeID, IngredientID = ingredients[21].IngredientID, Amount = 2, Unit = "tablespoons" }
            });

            // Caesar Salad ingredients
            recipeIngredients.AddRange(new[]
            {
                new RecipeIngredient { RecipeID = recipes[5].RecipeID, IngredientID = ingredients[13].IngredientID, Amount = 2, Unit = "cloves" },
                new RecipeIngredient { RecipeID = recipes[5].RecipeID, IngredientID = ingredients[7].IngredientID, Amount = 2, Unit = "yolks" },
                new RecipeIngredient { RecipeID = recipes[5].RecipeID, IngredientID = ingredients[19].IngredientID, Amount = 0.5, Unit = "cup" },
                new RecipeIngredient { RecipeID = recipes[5].RecipeID, IngredientID = ingredients[3].IngredientID, Amount = 0.5, Unit = "cup" }
            });

            await context.RecipeIngredients.AddRangeAsync(recipeIngredients);
            await context.SaveChangesAsync();
        }

        private static async Task<RecipeWorldUser?> CreateDemoUser(UserManager<RecipeWorldUser> userManager, SeedDataOptions options, ILogger logger)
        {
            // Create regular demo user
            RecipeWorldUser? existingUser = await userManager.FindByEmailAsync(options.DemoEmail);
            if (existingUser == null)
            {
                if (string.IsNullOrWhiteSpace(options.DemoPassword))
                {
                    logger.LogWarning("SeedData:DemoPassword is not configured; skipping demo user creation");
                }
                else
                {
                    var demoUser = new RecipeWorldUser
                    {
                        UserName = options.DemoEmail,
                        Email = options.DemoEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(demoUser, options.DemoPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(demoUser, Roles.User.ToString());
                        existingUser = demoUser;
                    }
                    else
                    {
                        logger.LogWarning("Failed to create demo user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            // Create admin user
            var existingAdmin = await userManager.FindByEmailAsync(options.AdminEmail);
            if (existingAdmin == null)
            {
                if (string.IsNullOrWhiteSpace(options.AdminPassword))
                {
                    logger.LogWarning("SeedData:AdminPassword is not configured; skipping admin user creation");
                }
                else
                {
                    var adminUser = new RecipeWorldUser
                    {
                        UserName = options.AdminEmail,
                        Email = options.AdminEmail,
                        EmailConfirmed = true
                    };

                    var adminResult = await userManager.CreateAsync(adminUser, options.AdminPassword);
                    if (adminResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, Roles.Admin.ToString());
                        await userManager.AddToRoleAsync(adminUser, Roles.Moderator.ToString());
                    }
                    else
                    {
                        logger.LogWarning("Failed to create admin user: {Errors}", string.Join("; ", adminResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            return existingUser;
        }
    }
}
