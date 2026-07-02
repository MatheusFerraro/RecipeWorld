using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using worldRecipeMvc.Models;
using worldRecipeMvc.Models.ViewModels;
using worldRecipeMvc.Services;
using worldRecipeMvc.Services.Errors;
using System.Security.Claims;

namespace worldRecipeMvc.Controllers
{
    public class IngredientsController : Controller
    {
        private const int DefaultPageSize = 10;

        private readonly IIngredientService _ingredientService;
        private readonly ILogger<IngredientsController> _logger;

        public IngredientsController(IIngredientService ingredientService, ILogger<IngredientsController> logger)
        {
            _ingredientService = ingredientService;
            _logger = logger;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User.IsInRole("Admin");

        // GET: Ingredients
        public async Task<IActionResult> Index(int pageNumber = 1, string? searchTerm = null)
        {
            var result = await _ingredientService.GetIngredientsAsync(pageNumber, DefaultPageSize, searchTerm);

            return View(new IngredientsIndexViewModel
            {
                Ingredients = result.Value,
                SearchTerm = searchTerm
            });
        }

        // GET: Ingredients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _ingredientService.GetIngredientByIdAsync(id.Value);
            return result.IsFailed ? NotFound() : View(result.Value);
        }

        [Authorize]
        // GET: Ingredients/Create
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
        // POST: Ingredients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ingredient ingredient)
        {
            if (ModelState.IsValid)
            {
                var result = await _ingredientService.CreateIngredientAsync(ingredient, UserId!);
                if (result.IsSuccess)
                {
                    TempData["Confirmation"] = "Created";
                    return RedirectToAction("ConfirmationIngredient", new { id = result.Value.IngredientID });
                }

                ModelState.AddModelError(nameof(ingredient.IngredientName), result.Errors.First().Message);
            }
            return View(ingredient);
        }

        [Authorize]
        // GET: Ingredients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _ingredientService.GetIngredientByIdAsync(id.Value);
            if (result.IsFailed)
            {
                return NotFound();
            }

            var ingredient = result.Value;
            if (!(IsAdmin || (ingredient.OwnerID == UserId && ingredient.IsApproved != true)))
            {
                _logger.LogWarning("User {UserId} attempted to edit ingredient {IngredientId} without permission", UserId, id);
                TempData["Error"] = "You can only edit your own unapproved ingredients.";
                return RedirectToAction(nameof(Index));
            }

            return View(ingredient);
        }

        [Authorize]
        // POST: Ingredients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("IngredientID,IngredientName,IngredientType,IngredientDetails")] Ingredient ingredient)
        {
            if (id == null || id != ingredient.IngredientID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var result = await _ingredientService.UpdateIngredientAsync(id.Value, ingredient, UserId!, IsAdmin);
                if (result.IsSuccess)
                {
                    TempData["Confirmation"] = "Modified";
                    return RedirectToAction("ConfirmationIngredient", new { id = ingredient.IngredientID });
                }

                if (result.HasError<NotFoundError>()) return NotFound();
                if (result.HasError<ForbiddenError>()) return Forbid();
                ModelState.AddModelError(nameof(ingredient.IngredientName), result.Errors.First().Message);
            }
            return View(ingredient);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _ingredientService.SetApprovalAsync(id, isApproved: true);
            if (result.IsFailed) return NotFound();

            TempData["Confirmation"] = "Approved";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _ingredientService.SetApprovalAsync(id, isApproved: false);
            if (result.IsFailed) return NotFound();

            TempData["Confirmation"] = "Rejected";
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        // GET: Ingredients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _ingredientService.GetIngredientByIdAsync(id.Value);
            if (result.IsFailed)
            {
                return NotFound();
            }

            var ingredient = result.Value;
            if (!(IsAdmin || (ingredient.OwnerID == UserId && ingredient.IsApproved != true)))
            {
                TempData["Error"] = "You can only delete your own unapproved ingredients.";
                return RedirectToAction(nameof(Index));
            }

            return View(ingredient);
        }

        [Authorize]
        // POST: Ingredients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id != null)
            {
                var result = await _ingredientService.DeleteIngredientAsync(id.Value, UserId!, IsAdmin);
                if (result.HasError<ForbiddenError>())
                {
                    return Forbid();
                }
                if (result.HasError<ConflictError>())
                {
                    TempData["Error"] = result.Errors.First().Message;
                    return RedirectToAction(nameof(Index));
                }
                if (result.IsSuccess)
                {
                    TempData["Confirmation"] = "Ingredient deleted successfully.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ConfirmationIngredient(int id)
        {
            var result = await _ingredientService.GetIngredientByIdAsync(id);
            return result.IsFailed ? NotFound() : View(result.Value);
        }
    }
}
