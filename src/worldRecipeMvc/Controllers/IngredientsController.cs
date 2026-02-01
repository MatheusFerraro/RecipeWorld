using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using System.Security.Claims;

namespace worldRecipeMvc.Controllers
{
    public class IngredientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IngredientsController> _logger;

        public IngredientsController(ApplicationDbContext context, ILogger<IngredientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Ingredients
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching all ingredients");
            return View(await _context.Ingredients
                .OrderBy(c => c.IngredientName)
                .ToListAsync());
        }

        // GET: Ingredients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details called with null id");
                return NotFound();
            }

            var ingredient = await _context.Ingredients
                .FirstOrDefaultAsync(m => m.IngredientID == id);

            if (ingredient == null)
            {
                _logger.LogWarning("Ingredient with ID {IngredientId} not found", id);
                return NotFound();
            }

            return View(ingredient);
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
                if (!IngredientExists(ingredient.IngredientName))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var newIngredient = new Ingredient
                    {
                        IngredientName = ingredient.IngredientName,
                        IngredientType = ingredient.IngredientType,
                        IngredientDetails = ingredient.IngredientDetails,
                        IsApproved = null!,
                        OwnerID = userId
                    };

                    _context.Add(newIngredient);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Ingredient '{IngredientName}' created with ID {IngredientId} by user {UserId}", newIngredient.IngredientName, newIngredient.IngredientID, userId);
                    TempData["Confirmation"] = "Created";
                    return RedirectToAction("ConfirmationIngredient", new { id = newIngredient.IngredientID });
                }
                else
                {
                    ModelState.AddModelError(nameof(ingredient.IngredientName), $"An Ingredient with the name {ingredient.IngredientName} already exists");
                    return View(ingredient);
                }
            }
            return View(ingredient);
        }

        [Authorize]
        // GET: Ingredients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit called with null id");
                return NotFound();
            }

            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                _logger.LogWarning("Ingredient with ID {IngredientId} not found for editing", id);
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = ingredient.OwnerID == userId;

            // Only allow edit if: Admin OR (owner AND not approved)
            if (!isAdmin && (!isOwner || ingredient.IsApproved == true))
            {
                _logger.LogWarning("User {UserId} attempted to edit ingredient {IngredientId} without permission", userId, id);
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
            if (id != ingredient.IngredientID)
            {
                return NotFound();
            }

            var existing = await _context.Ingredients.FindAsync(id);
            if (existing == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = existing.OwnerID == userId;

            // Only allow edit if: Admin OR (owner AND not approved)
            if (!isAdmin && (!isOwner || existing.IsApproved == true))
            {
                _logger.LogWarning("User {UserId} attempted to edit ingredient {IngredientId} without permission", userId, id);
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                bool nameExistOnOtherIngredient = await _context.Ingredients
                    .AnyAsync(i => i.IngredientName.ToUpper() == ingredient.IngredientName.ToUpper() && i.IngredientID != ingredient.IngredientID);

                if (!nameExistOnOtherIngredient)
                {
                    try
                    {
                        existing.IngredientName = ingredient.IngredientName;
                        existing.IngredientType = ingredient.IngredientType;
                        existing.IngredientDetails = ingredient.IngredientDetails;
                        // Reset approval status to pending (null) on edit
                        existing.IsApproved = null;

                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Ingredient {IngredientId} '{IngredientName}' updated", ingredient.IngredientID, ingredient.IngredientName);
                        TempData["Confirmation"] = "Modified";
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        if (!IngredientExists(ingredient.IngredientID))
                        {
                            return NotFound();
                        }
                        else
                        {
                            _logger.LogError(ex, "Concurrency error updating ingredient {IngredientId}", ingredient.IngredientID);
                            throw;
                        }
                    }
                    return RedirectToAction("ConfirmationIngredient", new { id = ingredient.IngredientID });
                }
                else
                {
                    ModelState.AddModelError(nameof(ingredient.IngredientName), $"An Ingredient with the name {ingredient.IngredientName} already exists");
                }
            }
            return View(ingredient);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                _logger.LogWarning("Ingredient with ID {IngredientId} not found for approval", id);
                return NotFound();
            }

            ingredient.IsApproved = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ingredient {IngredientId} '{IngredientName}' approved", id, ingredient.IngredientName);
            TempData["Confirmation"] = "Approved";
            return RedirectToAction(nameof(Index), new { id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                _logger.LogWarning("Ingredient with ID {IngredientId} not found for rejection", id);
                return NotFound();
            }

            ingredient.IsApproved = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ingredient {IngredientId} '{IngredientName}' rejected", id, ingredient.IngredientName);
            TempData["Confirmation"] = "Rejected";
            return RedirectToAction(nameof(Index), new { id });
        }

        [Authorize]
        // GET: Ingredients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete called with null id");
                return NotFound();
            }

            var ingredient = await _context.Ingredients
                .FirstOrDefaultAsync(m => m.IngredientID == id);

            if (ingredient == null)
            {
                _logger.LogWarning("Ingredient with ID {IngredientId} not found for deletion", id);
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = ingredient.OwnerID == userId;

            // Only allow delete if: Admin OR (owner AND not approved)
            if (!isAdmin && (!isOwner || ingredient.IsApproved == true))
            {
                _logger.LogWarning("User {UserId} attempted to delete ingredient {IngredientId} without permission", userId, id);
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
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient != null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool isAdmin = User.IsInRole("Admin");
                bool isOwner = ingredient.OwnerID == userId;

                // Only allow delete if: Admin OR (owner AND not approved)
                if (!isAdmin && (!isOwner || ingredient.IsApproved == true))
                {
                    _logger.LogWarning("User {UserId} attempted to delete ingredient {IngredientId} without permission", userId, id);
                    return Forbid();
                }

                // Check if any recipes use this ingredient
                var hasRecipes = await _context.RecipeIngredients.AnyAsync(ri => ri.IngredientID == id);
                if (hasRecipes)
                {
                    _logger.LogWarning("Attempted to delete ingredient {IngredientId} '{IngredientName}' but it's in use by recipes", id, ingredient.IngredientName);
                    TempData["Error"] = $"Cannot delete ingredient '{ingredient.IngredientName}' because it is used by one or more recipes.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Ingredients.Remove(ingredient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ingredient {IngredientId} '{IngredientName}' deleted", id, ingredient.IngredientName);
                TempData["Confirmation"] = "Ingredient deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult ConfirmationIngredient(int id)
        {
            var ingredient = _context.Ingredients.FirstOrDefault(i => i.IngredientID == id);
            if (ingredient == null)
            {
                _logger.LogWarning("Ingredient with ID {IngredientId} not found for confirmation page", id);
                return NotFound();
            }
            return View(ingredient);
        }

        private bool IngredientExists(int? id)
        {
            return _context.Ingredients.Any(e => e.IngredientID == id);
        }

        private bool IngredientExists(string? name)
        {
            return _context.Ingredients.Any(e => e.IngredientName == name);
        }
    }
}