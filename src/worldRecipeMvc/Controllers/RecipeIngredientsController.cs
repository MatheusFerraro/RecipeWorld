using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;

namespace worldRecipeMvc.Controllers
{
    public class RecipeIngredientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecipeIngredientsController> _logger;

        public RecipeIngredientsController(ApplicationDbContext context, ILogger<RecipeIngredientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: RecipeIngredients
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching all recipe ingredients");
            var applicationDbContext = _context.RecipeIngredients.Include(r => r.Ingredient).Include(r => r.Recipe);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: RecipeIngredients/Details/5
        public async Task<IActionResult> Details(int? recipeId, int? ingredientId)
        {
            if (recipeId == null || ingredientId == null)
            {
                _logger.LogWarning("Details called with null IDs");
                return NotFound();
            }

            var recipeIngredient = await _context.RecipeIngredients
                .Include(r => r.Ingredient)
                .Include(r => r.Recipe)
                .FirstOrDefaultAsync(m => m.RecipeID == recipeId && m.IngredientID == ingredientId);
            
            if (recipeIngredient == null)
            {
                _logger.LogWarning("RecipeIngredient with RecipeID {RecipeId} and IngredientID {IngredientId} not found", recipeId, ingredientId);
                return NotFound();
            }

            return View(recipeIngredient);
        }

        // GET: RecipeIngredients/Create
        public IActionResult Create()
        {
            _logger.LogInformation("Create recipe ingredient page accessed");
            ViewData["IngredientID"] = new SelectList(_context.Ingredients.Where(i => i.IsApproved == true), "IngredientID", "IngredientName");
            ViewData["RecipeID"] = new SelectList(_context.Recipes, "RecipeID", "RecipeName");
            return View();
        }

        // POST: RecipeIngredients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RecipeID,IngredientID,Amount,Unit")] RecipeIngredient recipeIngredient)
        {
            if (ModelState.IsValid)
            {
                // Check if the combination already exists
                if (RecipeIngredientExists(recipeIngredient.RecipeID ?? 0, recipeIngredient.IngredientID ?? 0))
                {
                    ModelState.AddModelError("", "This recipe-ingredient combination already exists.");
                    ViewData["IngredientID"] = new SelectList(_context.Ingredients.Where(i => i.IsApproved == true), "IngredientID", "IngredientName", recipeIngredient.IngredientID);
                    ViewData["RecipeID"] = new SelectList(_context.Recipes, "RecipeID", "RecipeName", recipeIngredient.RecipeID);
                    return View(recipeIngredient);
                }

                _context.Add(recipeIngredient);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Recipe ingredient created: RecipeID {RecipeId}, IngredientID {IngredientId}", 
                    recipeIngredient.RecipeID, recipeIngredient.IngredientID);
                return RedirectToAction(nameof(Index));
            }
            ViewData["IngredientID"] = new SelectList(_context.Ingredients.Where(i => i.IsApproved == true), "IngredientID", "IngredientName", recipeIngredient.IngredientID);
            ViewData["RecipeID"] = new SelectList(_context.Recipes, "RecipeID", "RecipeName", recipeIngredient.RecipeID);
            return View(recipeIngredient);
        }

        // GET: RecipeIngredients/Edit/5
        public async Task<IActionResult> Edit(int? recipeId, int? ingredientId)
        {
            if (recipeId == null || ingredientId == null)
            {
                _logger.LogWarning("Edit called with null IDs");
                return NotFound();
            }

            var recipeIngredient = await _context.RecipeIngredients
                .FirstOrDefaultAsync(ri => ri.RecipeID == recipeId && ri.IngredientID == ingredientId);
            
            if (recipeIngredient == null)
            {
                _logger.LogWarning("RecipeIngredient with RecipeID {RecipeId} and IngredientID {IngredientId} not found for editing", recipeId, ingredientId);
                return NotFound();
            }
            
            ViewData["IngredientID"] = new SelectList(_context.Ingredients.Where(i => i.IsApproved == true), "IngredientID", "IngredientName", recipeIngredient.IngredientID);
            ViewData["RecipeID"] = new SelectList(_context.Recipes, "RecipeID", "RecipeName", recipeIngredient.RecipeID);
            return View(recipeIngredient);
        }

        // POST: RecipeIngredients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int recipeId, int ingredientId, [Bind("RecipeID,IngredientID,Amount,Unit")] RecipeIngredient recipeIngredient)
        {
            if (recipeId != recipeIngredient.RecipeID || ingredientId != recipeIngredient.IngredientID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recipeIngredient);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Recipe ingredient updated: RecipeID {RecipeId}, IngredientID {IngredientId}", 
                        recipeId, ingredientId);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!RecipeIngredientExists(recipeId, ingredientId))
                    {
                        _logger.LogWarning("Recipe ingredient not found during update: RecipeID {RecipeId}, IngredientID {IngredientId}", recipeId, ingredientId);
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency error updating recipe ingredient: RecipeID {RecipeId}, IngredientID {IngredientId}", recipeId, ingredientId);
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IngredientID"] = new SelectList(_context.Ingredients.Where(i => i.IsApproved == true), "IngredientID", "IngredientName", recipeIngredient.IngredientID);
            ViewData["RecipeID"] = new SelectList(_context.Recipes, "RecipeID", "RecipeName", recipeIngredient.RecipeID);
            return View(recipeIngredient);
        }

        // GET: RecipeIngredients/Delete/5
        public async Task<IActionResult> Delete(int? recipeId, int? ingredientId)
        {
            if (recipeId == null || ingredientId == null)
            {
                _logger.LogWarning("Delete called with null IDs");
                return NotFound();
            }

            var recipeIngredient = await _context.RecipeIngredients
                .Include(r => r.Ingredient)
                .Include(r => r.Recipe)
                .FirstOrDefaultAsync(m => m.RecipeID == recipeId && m.IngredientID == ingredientId);
            
            if (recipeIngredient == null)
            {
                _logger.LogWarning("RecipeIngredient with RecipeID {RecipeId} and IngredientID {IngredientId} not found for deletion", recipeId, ingredientId);
                return NotFound();
            }

            return View(recipeIngredient);
        }

        // POST: RecipeIngredients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int recipeId, int ingredientId)
        {
            var recipeIngredient = await _context.RecipeIngredients
                .FirstOrDefaultAsync(ri => ri.RecipeID == recipeId && ri.IngredientID == ingredientId);
            
            if (recipeIngredient != null)
            {
                _context.RecipeIngredients.Remove(recipeIngredient);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Recipe ingredient deleted: RecipeID {RecipeId}, IngredientID {IngredientId}", 
                    recipeId, ingredientId);
            }
            else
            {
                _logger.LogWarning("Attempted to delete non-existent recipe ingredient: RecipeID {RecipeId}, IngredientID {IngredientId}", 
                    recipeId, ingredientId);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RecipeIngredientExists(int recipeId, int ingredientId)
        {
            return _context.RecipeIngredients.Any(e => e.RecipeID == recipeId && e.IngredientID == ingredientId);
        }
    }
}
