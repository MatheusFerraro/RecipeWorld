using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using worldRecipeMvc.Models;
using worldRecipeMvc.Models.ViewModels;
using worldRecipeMvc.Services;
using worldRecipeMvc.Services.Errors;
using System.Numerics;
using System.Security.Claims;

namespace worldRecipeMvc.Controllers
{
    public class RecipesController : Controller
    {
        private readonly IRecipeService _recipeService;
        private readonly ICategoryService _categoryService;
        private readonly IIngredientService _ingredientService;
        private readonly IImageStorageService _imageStorage;
        private readonly IRatingService _ratingService;
        private readonly IFavoriteService _favoriteService;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(
            IRecipeService recipeService,
            ICategoryService categoryService,
            IIngredientService ingredientService,
            IImageStorageService imageStorage,
            IRatingService ratingService,
            IFavoriteService favoriteService,
            ILogger<RecipesController> logger)
        {
            _recipeService = recipeService;
            _categoryService = categoryService;
            _ingredientService = ingredientService;
            _imageStorage = imageStorage;
            _ratingService = ratingService;
            _favoriteService = favoriteService;
            _logger = logger;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User.IsInRole("Admin");

        // GET: Recipes
        [AllowAnonymous]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 12, string? searchTerm = null, int? categoryFilter = null, string? statusFilter = null)
        {
            var result = await _recipeService.GetRecipesAsync(pageNumber, pageSize, searchTerm, categoryFilter, statusFilter, UserId, IsAdmin);
            var page = result.Value;

            var viewModel = new RecipesIndexViewModel
            {
                Recipes = page.Items,
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount,
                SearchTerm = searchTerm,
                CategoryFilter = categoryFilter,
                StatusFilter = statusFilter,
                Categories = await _categoryService.GetApprovedCategoriesAsync()
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
                return NotFound();
            }

            var result = await _recipeService.GetRecipeForViewingAsync(id.Value, UserId, IsAdmin);
            if (result.IsFailed)
            {
                return result.HasError<ForbiddenError>() ? Forbid() : NotFound();
            }

            var recipe = result.Value;
            var ingredients = recipe.RecipeIngredients.ToList();

            ViewData["Amount"] = FractionConversion(ingredients);
            ViewData["Temperature"] = TemperatureConversion(temp, recipe.Temperature);
            ViewData["PrepTime"] = RecipeService.TimeConversion(recipe.PrepTime);
            ViewData["CookTime"] = RecipeService.TimeConversion(recipe.CookTime);
            ViewData["TotalTime"] = RecipeService.TimeConversion(recipe.PrepTime + recipe.CookTime);

            // Ratings & favorites
            ViewBag.RatingSummary = await _ratingService.GetSummaryAsync(id.Value, UserId);
            ViewBag.Reviews = (await _ratingService.GetReviewsAsync(id.Value, 1, 10)).Value;
            ViewBag.FavoriteInfo = await _favoriteService.GetInfoAsync(id.Value, UserId);
            ViewBag.IsOwner = UserId != null && recipe.OwnerID == UserId;

            return View(recipe);
        }

        [Authorize]
        // GET: Recipes/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateRecipeViewModel { Recipe = new Recipe() };
            viewModel.Ingredients.Add(new RecipeIngredient());
            await PopulateCreateViewModelDropdowns(viewModel);
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
                viewModel.Ingredients.Add(new RecipeIngredient());
                await PopulateCreateViewModelDropdowns(viewModel);
                ViewBag.currentDate = DateOnly.FromDateTime(DateTime.Now);
                return View(viewModel);
            }

            if (action == "Save Recipe" && ModelState.IsValid)
            {
                // Handle image upload before creating the recipe
                if (imageFile != null && imageFile.Length > 0)
                {
                    var imageResult = await _imageStorage.SaveRecipeImageAsync(imageFile);
                    if (imageResult.IsFailed)
                    {
                        ModelState.AddModelError("ImageFile", imageResult.Errors.First().Message);
                        await PopulateCreateViewModelDropdowns(viewModel);
                        ViewBag.currentDate = DateOnly.FromDateTime(DateTime.Now);
                        return View(viewModel);
                    }
                    viewModel.Recipe.ImageUrl = imageResult.Value;
                }

                var result = await _recipeService.CreateRecipeAsync(viewModel.Recipe, viewModel.Ingredients, UserId!);
                if (result.IsSuccess)
                {
                    TempData["SaveConfirmation"] = "Recipe Successfully Saved";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("Recipe.RecipeName", result.Errors.First().Message);
            }

            await PopulateCreateViewModelDropdowns(viewModel);
            ViewBag.currentDate = DateOnly.FromDateTime(DateTime.Now);
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

            var result = await _recipeService.GetRecipeWithDetailsAsync(id.Value);
            if (result.IsFailed)
            {
                return NotFound();
            }

            var recipeToEdit = result.Value;
            bool isOwner = recipeToEdit.OwnerID == UserId;
            if (!(IsAdmin || isOwner))
            {
                _logger.LogWarning("User {UserId} attempted unauthorized edit of recipe {RecipeId}", UserId, id);
                return Forbid();
            }

            bool ownerCanChangeStatus = IsAdmin;
            if (!IsAdmin && isOwner)
            {
                bool allIngredientsApproved = recipeToEdit.RecipeIngredients.All(ri => ri.Ingredient != null && ri.Ingredient.IsApproved == true);
                bool categoryApproved = recipeToEdit.Category == null || recipeToEdit.Category.IsApproved == true;
                ownerCanChangeStatus = allIngredientsApproved && categoryApproved;
            }

            var viewModel = new EditRecipeViewModel
            {
                Recipe = recipeToEdit,
                Ingredients = recipeToEdit.RecipeIngredients.ToList(),
                StatusList = ownerCanChangeStatus
                    ? new SelectList(RecipeStatus.All, recipeToEdit.Status)
                    : new SelectList(new[] { recipeToEdit.Status }, recipeToEdit.Status)
            };

            await PopulateEditViewModelDropdowns(viewModel, keepStatusList: true);

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

            if (action == "Add Ingredient")
            {
                viewModel.Ingredients.Add(new RecipeIngredient { RecipeID = viewModel.Recipe.RecipeID });
                await PopulateEditViewModelDropdowns(viewModel);
                return View(viewModel);
            }

            if (action.StartsWith("Remove_"))
            {
                int indexRemove = int.Parse(action.Split('_')[1]);
                if (indexRemove >= 0 && indexRemove < viewModel.Ingredients.Count)
                {
                    viewModel.Ingredients.RemoveAt(indexRemove);
                }
                await PopulateEditViewModelDropdowns(viewModel);
                return View(viewModel);
            }

            if (action == "Update Recipe" && ModelState.IsValid)
            {
                // Handle image upload before updating the recipe
                if (imageFile != null && imageFile.Length > 0)
                {
                    var imageResult = await _imageStorage.SaveRecipeImageAsync(imageFile);
                    if (imageResult.IsFailed)
                    {
                        ModelState.AddModelError("ImageFile", imageResult.Errors.First().Message);
                        await PopulateEditViewModelDropdowns(viewModel);
                        return View(viewModel);
                    }

                    var oldImage = (await _recipeService.GetRecipeWithDetailsAsync(id)).ValueOrDefault?.ImageUrl;
                    _imageStorage.DeleteRecipeImage(oldImage);
                    viewModel.Recipe.ImageUrl = imageResult.Value;
                }

                var result = await _recipeService.UpdateRecipeAsync(id, viewModel.Recipe, viewModel.Ingredients, UserId!, IsAdmin);
                if (result.IsSuccess)
                {
                    TempData["SaveConfirmation"] = "Recipe Successfully Updated";
                    return RedirectToAction(nameof(Index));
                }

                if (result.HasError<NotFoundError>())
                {
                    return NotFound();
                }
                if (result.HasError<ForbiddenError>())
                {
                    return Forbid();
                }
                ModelState.AddModelError("Recipe.RecipeName", result.Errors.First().Message);
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

            var result = await _recipeService.GetRecipeWithDetailsAsync(id.Value);
            if (result.IsFailed)
            {
                return NotFound();
            }

            if (!(IsAdmin || result.Value.OwnerID == UserId))
            {
                return Forbid();
            }

            return View(result.Value);
        }

        [Authorize]
        // POST: Recipes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id != null)
            {
                var result = await _recipeService.DeleteRecipeAsync(id.Value, UserId!, IsAdmin);
                if (result.HasError<ForbiddenError>())
                {
                    return Forbid();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Recipes/ChangeStatus/5
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangeStatus(int? id)
        {
            if (id == null) return NotFound();

            var result = await _recipeService.GetRecipeWithDetailsAsync(id.Value);
            if (result.IsFailed) return NotFound();

            var recipe = result.Value;
            if (!(IsAdmin || recipe.OwnerID == UserId)) return Forbid();

            ViewBag.StatusList = new SelectList(RecipeStatus.All, recipe.Status);
            return View("ChangeStatus", recipe);
        }

        // POST: Recipes/ChangeStatus/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var result = await _recipeService.ChangeStatusAsync(id, status, UserId!, IsAdmin);
            if (result.IsSuccess)
            {
                TempData["SaveConfirmation"] = "Status updated.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (result.HasError<NotFoundError>()) return NotFound();
            if (result.HasError<ForbiddenError>()) return Forbid();

            // Invalid status value: redisplay the form with the error
            var recipeResult = await _recipeService.GetRecipeWithDetailsAsync(id);
            if (recipeResult.IsFailed) return NotFound();

            ModelState.AddModelError("Status", result.Errors.First().Message);
            ViewBag.StatusList = new SelectList(RecipeStatus.All, recipeResult.Value.Status);
            return View("ChangeStatus", recipeResult.Value);
        }

        private async Task PopulateCreateViewModelDropdowns(CreateRecipeViewModel viewModel)
        {
            var allCategories = await _categoryService.GetAllCategoriesAsync();
            var allIngredients = await _ingredientService.GetAllIngredientsAsync();

            viewModel.CategoryList = new SelectList(allCategories, "CategoryID", "CategoryName", viewModel.Recipe.CategoryID);
            viewModel.IngredientList = new SelectList(allIngredients, "IngredientID", "IngredientName");

            DisablePendingItems(viewModel.IngredientList, allIngredients.Where(i => i.IsApproved == null).Select(i => i.IngredientName));
            DisablePendingItems(viewModel.CategoryList, allCategories.Where(c => c.IsApproved == null).Select(c => c.CategoryName));
        }

        private async Task PopulateEditViewModelDropdowns(EditRecipeViewModel viewModel, bool keepStatusList = false)
        {
            var allCategories = await _categoryService.GetAllCategoriesAsync();
            var allIngredients = await _ingredientService.GetAllIngredientsAsync();

            viewModel.CategoryList = new SelectList(allCategories, "CategoryID", "CategoryName", viewModel.Recipe.CategoryID);
            viewModel.IngredientList = new SelectList(allIngredients, "IngredientID", "IngredientName");
            if (!keepStatusList)
            {
                viewModel.StatusList = new SelectList(RecipeStatus.All, viewModel.Recipe.Status);
            }

            DisablePendingItems(viewModel.IngredientList, allIngredients.Where(i => i.IsApproved == null).Select(i => i.IngredientName));
            DisablePendingItems(viewModel.CategoryList, allCategories.Where(c => c.IsApproved == null).Select(c => c.CategoryName));
        }

        private static void DisablePendingItems(SelectList list, IEnumerable<string?> pendingNames)
        {
            var pending = pendingNames.Where(n => n != null).ToHashSet();
            foreach (SelectListItem item in list)
            {
                if (pending.Contains(item.Text))
                {
                    item.Disabled = true;
                }
            }
        }

        public static int? TemperatureConversion(bool? type, int? temperature)
        {
            if (type == null || type == true)
            {
                return temperature;
            }

            return (int?)((temperature - 32) / 1.8);
        }

        private static List<string?> FractionConversion(List<RecipeIngredient?> ingredients)
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
    }
}
