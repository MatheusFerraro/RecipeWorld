using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using worldRecipeMvc.Models.ViewModels;
using System.Numerics;
using System.Security.Claims;

namespace worldRecipeMvc.Controllers
{
    
    public class RecipesController : Controller
    {

        
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<RecipesController> _logger;
        private readonly IConfiguration _configuration;

        public RecipesController(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<RecipesController> logger, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _configuration = configuration;
        }

        // GET: Recipes
        [AllowAnonymous]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 12, string? searchTerm = null, int? categoryFilter = null, string? statusFilter = null)
        {
            _logger.LogInformation("Fetching recipes - Page: {PageNumber}, Search: {SearchTerm}", pageNumber, searchTerm);
            
            var query = _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Owner)
                .AsQueryable();

            
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => r.RecipeName!.Contains(searchTerm));
            }

            
            if (categoryFilter.HasValue && categoryFilter.Value > 0)
            {
                query = query.Where(r => r.CategoryID == categoryFilter.Value);
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(r => r.Status == statusFilter);
            }
            else
            {
                // Default: show only published recipes for non-authenticated users
                if (!User.Identity.IsAuthenticated)
                {
                    query = query.Where(r => r.Status == "Public");
                }
                else if (!User.IsInRole("Admin"))
                {
                    // For authenticated users (non-admin), show their own recipes + published recipes
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    query = query.Where(r => r.Status == "Public" || r.OwnerID == userId);
                }
            }

            var totalCount = await query.CountAsync();

            var recipes = await query
                .OrderByDescending(r => r.RecipeID)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new DisplayRecipeViewModel
                {
                    RecipeID = s.RecipeID,
                    Category = s.Category,
                    RecipeName = s.RecipeName,
                    PrepTime = TimeConversion(s.PrepTime),
                    CookTime = TimeConversion(s.CookTime),
                    Tips = s.Tips,
                    NumberOfServings = s.NumberOfServings,
                    Status = s.Status,
                    Owner = s.Owner,
                    Instructions = s.Instructions,
                    Temperature = s.Temperature,
                    ImageUrl = s.ImageUrl
                })
                .ToListAsync();

            // Get categories for filter dropdown
            var categories = await _context.Categories
                .Where(c => c.IsApproved == true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var viewModel = new RecipesIndexViewModel
            {
                Recipes = recipes,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                SearchTerm = searchTerm,
                CategoryFilter = categoryFilter,
                StatusFilter = statusFilter,
                Categories = categories
            };

            return View(viewModel);
        }

        // GET: Recipes/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id, bool? temp)
        {
            ViewData["Temp"] = (temp == null || temp == true) ? false : true;
            ViewData["TempLetter"] = (temp == null || temp == true) ? "F" : "C";
            ViewData["CurrentTemp"] = temp;

            if (id == null)
            {
                _logger.LogWarning("Details called with null id");
                return NotFound();
            }

            var recipe = await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Owner)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(m => m.RecipeID == id);

            if (recipe == null)
            {
                _logger.LogWarning("Recipe with ID {RecipeId} not found", id);
                return NotFound();
            }

            // Restrict viewing of drafts and private recipes to owner or admin
            var status = recipe.Status?.ToLower();
            if (status == "draft" || status == "private")
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!(User.Identity.IsAuthenticated && (User.IsInRole("Admin") || recipe.OwnerID == userId)))
                {
                    _logger.LogWarning("User {UserId} attempted unauthorized access to recipe {RecipeId}", userId, id);
                    return Forbid();
                }
            }

            var ingredients = await _context.RecipeIngredients
                .Where(ri => ri.RecipeID == id)
                .ToListAsync();

           
            List<string?> converted = FractionConversion(ingredients, id);
            ViewData["Amount"] = converted;
            ViewData["Temperature"] = TemperatureConversion(temp, recipe.Temperature);
            ViewData["PrepTime"] = TimeConversion(recipe.PrepTime);
            ViewData["CookTime"] = TimeConversion(recipe.CookTime);
            ViewData["TotalTime"] = TimeConversion((recipe.PrepTime + recipe.CookTime));
            
            return View(recipe);
        }

        [Authorize]
        // GET: Recipes/Create

        public async Task<IActionResult> Create()
        {
            var allCategories =
               await _context.Categories
               .OrderBy(c => c.CategoryName)
               .ToListAsync();

            var allingredients = 
                await _context.Ingredients
                .OrderBy(i => i.IngredientName)
                .ToListAsync();

            var viewModel = new CreateRecipeViewModel
            {
                Recipe = new Recipe(),
                CategoryList = new SelectList(allCategories, "CategoryID", "CategoryName"),
                IngredientList = new SelectList(allingredients, "IngredientID", "IngredientName")
            };

            foreach (SelectListItem item in viewModel.IngredientList)
            {
                var ingredient = allingredients.Where(c => c.IngredientName == item.Text && c.IsApproved == null);
                if (ingredient != null && ingredient.Any())
                {
                    item.Disabled = true;
                }
            }

            foreach (SelectListItem item in viewModel.CategoryList)
            {
                var category = allCategories.Where(c => c.CategoryName == item.Text && c.IsApproved == null);
                if (category != null && category.Any())
                {
                    item.Disabled = true;
                }
            }

            viewModel.Ingredients.Add(new RecipeIngredient());
            ViewBag.currentDate = DateOnly.FromDateTime(DateTime.Now);
            
            return View(viewModel);
        }

        [Authorize]
        // POST: Recipes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRecipeViewModel viewModel, string action, IFormFile? imageFile)
        {

            if (action == "Add Ingredient") 
            { 
                var newIngredient = new RecipeIngredient();
                viewModel.Ingredients.Add(newIngredient);
                await PopulateCreateViewModelDropdowns(viewModel);
                ViewBag.currentDate = DateOnly.FromDateTime(DateTime.Now);
                return View(viewModel);
            }

            if(action == "Save Recipe") 
            {
                if (ModelState.IsValid)
                {
                    if (!RecipeExists(viewModel.Recipe.RecipeName))
                    {

                        var recipeToSave = viewModel.Recipe;
                        recipeToSave.Status = "Draft";
                        
                        // Handle image upload
                        if (imageFile != null && imageFile.Length > 0)
                        {
                            var imageUrl = await SaveRecipeImage(imageFile);
                            if (imageUrl != null)
                            {
                                recipeToSave.ImageUrl = imageUrl;
                            }
                            else
                            {
                                recipeToSave.ImageUrl = "/websiteImages/RecipeDefaultImage.png";
                            }
                        }
                        else if (string.IsNullOrWhiteSpace(recipeToSave.ImageUrl))
                        {
                            recipeToSave.ImageUrl = "/websiteImages/RecipeDefaultImage.png";
                        }

                        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        recipeToSave.OwnerID = userId;

                        _context.Add(recipeToSave);
                        await _context.SaveChangesAsync();

                        var addedIngredientsIds = new HashSet<int>();

                        foreach (var ingredient in viewModel.Ingredients)
                        {
                            if(ingredient.IngredientID.HasValue && ingredient.IngredientID.Value > 0 
                                && ingredient.Amount.HasValue && ingredient.Amount.Value > 0)
                            {
                                int currentIngredientId = ingredient.IngredientID.Value;

                                if(!addedIngredientsIds.Contains(currentIngredientId))
                                {
                                    ingredient.RecipeID = recipeToSave.RecipeID.Value;
                                    _context.Add(ingredient);

                                    addedIngredientsIds.Add(currentIngredientId);
                                }
                                
                            }
                        }
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Recipe {RecipeName} created by user {UserId}", recipeToSave.RecipeName, userId);
                        TempData["SaveConfirmation"] = "Recipe Successfully Saved";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError(nameof(Recipe.RecipeName), $"A Recipe with the name {viewModel.Recipe.RecipeName} already exists");

                    }
                }


            }
            var adminApprovedCategories_fail = 
                await _context.Categories
                .Where(c => c.IsApproved == true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            
            var allIngredients_fail = 
                await _context.Ingredients
                .OrderBy(i => i.IngredientName)
                .ToListAsync();

            viewModel.CategoryList = new SelectList(adminApprovedCategories_fail, "CategoryID", "CategoryName", viewModel.Recipe.CategoryID);
            viewModel.IngredientList = new SelectList(allIngredients_fail, "IngredientID", "IngredientName");

            return View(viewModel);

        }

        [Authorize]
        // GET: Recipes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipeToEdit = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.RecipeID == id);

            if (recipeToEdit == null)
            {
                _logger.LogWarning("Recipe with ID {RecipeId} not found for editing", id);
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = recipeToEdit.OwnerID == userId;
            if (!(isAdmin || isOwner))
            {
                _logger.LogWarning("User {UserId} attempted unauthorized edit of recipe {RecipeId}", userId, id);
                return Forbid();
            }

            bool ownerCanChangeStatus = false;
            if (isAdmin)
            {
                ownerCanChangeStatus = true;
            }
            else if (isOwner)
            {
                bool allIngredientsApproved = recipeToEdit.RecipeIngredients.All(ri => ri.Ingredient != null && ri.Ingredient.IsApproved == true);
                bool categoryApproved = recipeToEdit.Category == null || recipeToEdit.Category.IsApproved == true;
                ownerCanChangeStatus = allIngredientsApproved && categoryApproved;
            }

                var allCategories =
                    await _context.Categories
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();


            var allIngredients =
              await _context.Ingredients
              .OrderBy(i => i.IngredientName)
              .ToListAsync();

            var viewModel = new EditRecipeViewModel
            {
                Recipe = recipeToEdit,
                Ingredients = recipeToEdit.RecipeIngredients.ToList(),
                CategoryList = new SelectList(allCategories, "CategoryID", "CategoryName", recipeToEdit.CategoryID),
                IngredientList = new SelectList(allIngredients, "IngredientID", "IngredientName"),
                StatusList = ownerCanChangeStatus ? new SelectList(RecipeStatus.All, recipeToEdit.Status) : new SelectList(new[] { recipeToEdit.Status }, recipeToEdit.Status)

            };

            foreach (SelectListItem item in viewModel.IngredientList)
            {
                var ingredient = allIngredients.Where(c => c.IngredientName == item.Text && c.IsApproved == null);
                if (ingredient != null && ingredient.Any())
                {
                    item.Disabled = true;
                }
            }

            foreach (SelectListItem item in viewModel.CategoryList)
            {
                var category = allCategories.Where(c => c.CategoryName == item.Text && c.IsApproved == null);
                if (category != null && category.Any())
                {
                    item.Disabled = true;
                }
            }

            if (viewModel.Ingredients.Count == 0)
            {
                viewModel.Ingredients.Add(new RecipeIngredient() { RecipeID = recipeToEdit.RecipeID });
            }
        
            ViewBag.currentDate = DateOnly.FromDateTime(DateTime.Now);
            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditRecipeViewModel viewModel, string action, IFormFile? imageFile)
        {
            if (id != viewModel.Recipe.RecipeID)
            {
                return NotFound();
            }

            var recipeInDb = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.RecipeID == id);

            if (recipeInDb == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = recipeInDb.OwnerID == userId;
            if (!(isAdmin || isOwner))
            {
                return Forbid();
            }

            if (!isAdmin && isOwner)
            {
                bool allIngredientsApproved = recipeInDb.RecipeIngredients.All(ri => ri.Ingredient != null && ri.Ingredient.IsApproved == true);
                bool categoryApproved = recipeInDb.Category == null || recipeInDb.Category.IsApproved == true;
                bool ownerCanChangeStatus = allIngredientsApproved && categoryApproved;

                if (!ownerCanChangeStatus)
                {
                    viewModel.Recipe.Status = recipeInDb.Status;
                }
            }

            if (action == "Add Ingredient")
            {
                var newIngredient = new RecipeIngredient { RecipeID = viewModel.Recipe.RecipeID };
                viewModel.Ingredients.Add(newIngredient);

                await PopulateEditViewModelDropdowns(viewModel);
                return View(viewModel);
            }

            if (action.StartsWith("Remove_"))
            {
                int indexRemove = int.Parse(action.Split('_')[1]);

                if(indexRemove >= 0 && indexRemove < viewModel.Ingredients.Count)
                {
                    viewModel.Ingredients.RemoveAt(indexRemove);
                }
                await PopulateEditViewModelDropdowns(viewModel);
                return View(viewModel);

            }

            if (action == "Update Recipe")
            {
                if (id != viewModel.Recipe.RecipeID)
                {
                    return NotFound();
                }
                bool nameExistsOnOtherCategory = await _context.Recipes
                      .AnyAsync(r => r.RecipeName
                      .ToUpper() == viewModel.Recipe.RecipeName
                      .ToUpper() && r.RecipeID != viewModel.Recipe.RecipeID);

                if (nameExistsOnOtherCategory)
                {
                    ModelState.AddModelError("Recipe.RecipeName", $"A Recipe with the name {viewModel.Recipe.RecipeName} already exists");
                }

                if (ModelState.IsValid)
                {

                    try
                    {
                        var recipeUpdate = await _context.Recipes
                            .Include(r => r.RecipeIngredients)
                                .ThenInclude(ri => ri.Ingredient)
                            .FirstOrDefaultAsync(r => r.RecipeID == id);

                        if (recipeUpdate == null) return NotFound();

                        recipeUpdate.RecipeName = viewModel.Recipe.RecipeName;
                        recipeUpdate.CategoryID = viewModel.Recipe.CategoryID;
                        recipeUpdate.PrepTime = viewModel.Recipe.PrepTime;
                        recipeUpdate.CookTime = viewModel.Recipe.CookTime;
                        recipeUpdate.Tips = viewModel.Recipe.Tips;
                        recipeUpdate.NumberOfServings = viewModel.Recipe.NumberOfServings;
                        recipeUpdate.Instructions = viewModel.Recipe.Instructions;
                        recipeUpdate.Temperature = viewModel.Recipe.Temperature;

                        // Handle image upload for edit
                        if (imageFile != null && imageFile.Length > 0)
                        {
                            // Delete old image if it's not the default
                            if (!string.IsNullOrEmpty(recipeUpdate.ImageUrl) && 
                                !recipeUpdate.ImageUrl.Contains("RecipeDefaultImage"))
                            {
                                DeleteRecipeImage(recipeUpdate.ImageUrl);
                            }

                            var imageUrl = await SaveRecipeImage(imageFile);
                            if (imageUrl != null)
                            {
                                recipeUpdate.ImageUrl = imageUrl;
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(viewModel.Recipe.ImageUrl))
                        {
                            recipeUpdate.ImageUrl = viewModel.Recipe.ImageUrl;
                        }

                        if (!string.IsNullOrEmpty(viewModel.Recipe.Status))
                        {
                            recipeUpdate.Status = viewModel.Recipe.Status;
                        }

                        foreach( var oldIngredient in recipeUpdate.RecipeIngredients.ToList())
                        {
                            _context.RecipeIngredients.Remove(oldIngredient);
                        }

                        var addedIngredientsIds = new HashSet<int>();
                        foreach(var newIngredient in viewModel.Ingredients)
                        {
                            if(newIngredient.IngredientID.HasValue && 
                               newIngredient.IngredientID.Value > 0 
                               && newIngredient.Amount.HasValue && newIngredient.Amount.Value > 0)
                            {
                                int currentIngredientId = newIngredient.IngredientID.Value;
                                if (!addedIngredientsIds.Contains(currentIngredientId))
                                {
                                    newIngredient.RecipeID = recipeUpdate.RecipeID.Value;

                                    var ingredientToAdd = new RecipeIngredient
                                    {
                                        RecipeID = recipeUpdate.RecipeID.Value,
                                        IngredientID = newIngredient.IngredientID.Value,
                                        Amount = newIngredient.Amount,
                                        Unit = newIngredient.Unit
                                    };

                                    _context.RecipeIngredients.Add(ingredientToAdd);
                                    addedIngredientsIds.Add(currentIngredientId);
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Recipe {RecipeId} updated by user {UserId}", id, userId);
                        TempData["SaveConfirmation"] = "Recipe Successfully Updated and set to Draft for approval";
                        return RedirectToAction(nameof(Index));

                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        if (!RecipeExists(viewModel.Recipe.RecipeID))
                        {
                            return NotFound();
                        }
                        else 
                        {
                           _logger.LogError(ex, "Concurrency error updating recipe {RecipeId}", id);
                           throw;
                        }
                    }

                }

            }

            await PopulateEditViewModelDropdowns(viewModel);
            return View(viewModel);


        }

        [Authorize]
        // GET: Recipes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipe = await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Owner)
                .FirstOrDefaultAsync(m => m.RecipeID == id);
            if (recipe == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!(User.IsInRole("Admin") || recipe.OwnerID == userId))
            {
                return Forbid();
            }

            return View(recipe);
        }

        [Authorize]
        // POST: Recipes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe != null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!(User.IsInRole("Admin") || recipe.OwnerID == userId))
                {
                    return Forbid();
                }

                _context.Recipes.Remove(recipe);
                _logger.LogInformation("Recipe {RecipeId} deleted by user {UserId}", id, userId);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Recipes/ChangeStatus/5
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangeStatus(int? id)
        {
            if (id == null) return NotFound();

            var recipe = await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Owner)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.RecipeID == id);
            if (recipe == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!(User.IsInRole("Admin") || recipe.OwnerID == userId)) return Forbid();

            ViewBag.StatusList = new SelectList(RecipeStatus.All, recipe.Status);
            return View("ChangeStatus", recipe);
        }

        // POST: Recipes/ChangeStatus/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var recipe = await _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Owner)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.RecipeID == id);
            if (recipe == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = recipe.OwnerID == userId;
            if (!(isAdmin || isOwner)) return Forbid();

            // Basic validation: only allow values in RecipeStatus.All
            if (!RecipeStatus.All.Contains(status))
            {
                ModelState.AddModelError("Status", "Invalid status selection.");
                ViewBag.StatusList = new SelectList(RecipeStatus.All, recipe.Status);
                return View("ChangeStatus", recipe);
            }

          
            recipe.Status = status;
            _logger.LogInformation("Recipe {RecipeId} status changed to {Status} by user {UserId}", id, status, userId);
            
            await _context.SaveChangesAsync();
            TempData["SaveConfirmation"] = "Status updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private bool RecipeExists(int? id)
        {
            return _context.Recipes.Any(e => e.RecipeID == id);
        }

        private bool RecipeExists(string? name)
        {
            if(name == null)
            {
                return false;
            }

            return _context.Recipes.Any(e => e.RecipeName.ToUpper() == name.ToUpper());
        }

        private async Task PopulateCreateViewModelDropdowns(CreateRecipeViewModel viewModel)
        {
            var allCategories = await _context.Categories
                .OrderBy(c => c.CategoryName).ToListAsync();
            var allIngredients = await _context.Ingredients
                .OrderBy(i => i.IngredientName).ToListAsync();

            viewModel.CategoryList = new SelectList(allCategories, "CategoryID", "CategoryName", viewModel.Recipe.CategoryID);
            viewModel.IngredientList = new SelectList(allIngredients, "IngredientID", "IngredientName");

            foreach (SelectListItem item in viewModel.IngredientList)
            {
                var ingredient = allIngredients.Where(c => c.IngredientName == item.Text && c.IsApproved == null);
                if (ingredient != null && ingredient.Any())
                {
                    item.Disabled = true;
                }
            }

            foreach (SelectListItem item in viewModel.CategoryList)
            {
                var category = allCategories.Where(c => c.CategoryName == item.Text && c.IsApproved == null);
                if (category != null && category.Any())
                {
                    item.Disabled = true;
                }
            }
        }
        private async Task PopulateEditViewModelDropdowns(EditRecipeViewModel viewModel)
        {
            var allCategories = await _context.Categories
                 .OrderBy(c => c.CategoryName).ToListAsync();

            var allIngredients = await _context.Ingredients
                .OrderBy(i => i.IngredientName).ToListAsync();

            var categorySelectItems = allCategories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryID.ToString(),
                    Text = c.CategoryName,
                    Disabled = (c.IsApproved != true),
                    Selected = (c.CategoryID == viewModel.Recipe.CategoryID)
                })
                .ToList();

            viewModel.CategoryList = new SelectList(categorySelectItems, "Value", "Text", viewModel.Recipe.CategoryID);
            viewModel.IngredientList = new SelectList(allIngredients, "IngredientID", "IngredientName");
            viewModel.StatusList = new SelectList(RecipeStatus.All, viewModel.Recipe.Status);

            foreach (SelectListItem item in viewModel.IngredientList)
            {
                var ingredient = allIngredients.Where(c => c.IngredientName == item.Text && c.IsApproved == null);
                if (ingredient != null && ingredient.Any())
                {
                    item.Disabled = true;
                }
            }

            foreach (SelectListItem item in viewModel.CategoryList)
            {
                var categories = allCategories.Where(c => c.CategoryName == item.Text && c.IsApproved == null);
                if (categories != null && categories.Any())
                {
                    item.Disabled = true;
                }
            }

        }
        public static string? TimeConversion(double? time)
        {
            if (time >= 60)
            {
                double hours = (int)(time / 60);
                double minutes = (double)(time % 60);
                return $"{hours} hrs {minutes} mins";
            }
            else
            {
                return $"{time} mins";
            }
        }

        public static int? TemperatureConversion(bool? type, int? temperature)
        {
            if (type == null || type == true)
            {
                return temperature;
                
            }
            else
            {
                return (int?)((temperature - 32) / 1.8);
            } 
        }

        private List<string?> FractionConversion(List<RecipeIngredient?> ingredients, int? id)
        {
            List<string?> converted = new List<string?>();

            foreach (var ingredient in ingredients)
            {

                if (ingredient?.Amount == null)
                {
                    converted.Add("N/A");
                    continue;
                }

                string amountSt = ingredient.Amount.ToString()!;
                string[] location = amountSt.Split(".");

                if (location.Length > 1)
                {
                    double wholeNumber = double.Parse(location[0]);
                    string fraction = location[1];
                    double length = fraction.Length;
                    double? num = Math.Pow(10, length);
                    double? pow = ingredient.Amount * num;

                    BigInteger gcd = BigInteger.GreatestCommonDivisor((BigInteger)num, (BigInteger)pow);

                    double numerator = (double)((BigInteger)pow / gcd);
                    double denomenator = (double)((BigInteger)num / gcd);
                    numerator = numerator % denomenator;

                    converted.Add($"{wholeNumber:#} {numerator}/{denomenator}");
                }
                else
                {
                    converted.Add($"{ingredient.Amount}");
                }
            }

            return converted;

        }

        // Helper method to save uploaded recipe image
        private async Task<string?> SaveRecipeImage(IFormFile imageFile)
        {
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("Invalid image file type: {Extension}", extension);
                    ModelState.AddModelError("ImageFile", "Only image files (jpg, jpeg, png, gif, webp) are allowed.");
                    return null;
                }

                // Validate file size (max 5MB)
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning("Image file too large: {Size} bytes", imageFile.Length);
                    ModelState.AddModelError("ImageFile", "Image file size must be less than 5MB.");
                    return null;
                }

                // Create unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                
                // Get configured upload path or fall back to default
                var uploadsFolder = _configuration["ImageUpload:StoragePath"] 
                    ?? Path.Combine(_environment.WebRootPath, "images", "recipes");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                _logger.LogInformation("Recipe image saved: {FileName}", uniqueFileName);
                return $"/images/recipes/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving recipe image");
                return null;
            }
        }

        // Helper method to delete old recipe image
        private void DeleteRecipeImage(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl) || imageUrl.Contains("RecipeDefaultImage"))
                {
                    return;
                }

                var fileName = Path.GetFileName(imageUrl);
                
                // Get configured upload path or fall back to default
                var uploadsFolder = _configuration["ImageUpload:StoragePath"] 
                    ?? Path.Combine(_environment.WebRootPath, "images", "recipes");
                var filePath = Path.Combine(uploadsFolder, fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Deleted old recipe image: {FileName}", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe image: {ImageUrl}", imageUrl);
            }
        }
    }
}

