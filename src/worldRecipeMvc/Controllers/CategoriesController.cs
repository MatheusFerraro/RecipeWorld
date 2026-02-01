using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.Models;
using System.Security.Claims;


namespace worldRecipeMvc.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;
        
        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Categories
        public async Task<IActionResult> Index(string sortOrder)
        {
            _logger.LogInformation("Fetching categories with sort order: {SortOrder}", sortOrder ?? "default");
            
            ViewData["NameSortParam"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";

            var categories = from c in _context.Categories
                             select c;

            switch (sortOrder) 
            {
                case "name_desc":
                    categories = categories.OrderByDescending(c => c.CategoryName);
                    break;
                default:
                    categories = categories.OrderBy(c => c.CategoryName);
                    break;
            }

            return View(await categories.AsNoTracking().ToListAsync());
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details called with null id");
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.CategoryID == id);
            
            if (category == null)
            {
                _logger.LogWarning("Category with ID {CategoryId} not found", id);
                return NotFound();
            }

            return View(category);
        }

        [Authorize]
        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                if (!CategoryExists(category.CategoryName))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var newCategory = new Category
                    {
                        CategoryName = category.CategoryName,
                        CategoryDescription = category.CategoryDescription,
                        IsApproved = null!,
                        OwnerID = userId
                    };

                    _context.Add(newCategory);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Category '{CategoryName}' created with ID {CategoryId} by user {UserId}", newCategory.CategoryName, newCategory.CategoryID, userId);
                    TempData["Confirmation"] = "Created";
                    return RedirectToAction("ConfirmationCategory", new { id = newCategory.CategoryID });
                }
                else
                {
                   
                    ModelState.AddModelError(nameof(category.CategoryName), $"A Category with the name {category.CategoryName} already exists");
                    return View(category); 
                }
            }
            return View(category);
        }

        [Authorize]
        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit called with null id");
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category with ID {CategoryId} not found for editing", id);
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = category.OwnerID == userId;

            // Only allow edit if: Admin OR (owner AND not approved)
            if (!isAdmin && (!isOwner || category.IsApproved == true))
            {
                _logger.LogWarning("User {UserId} attempted to edit category {CategoryId} without permission", userId, id);
                TempData["Error"] = "You can only edit your own unapproved categories.";
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        [Authorize]
        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("CategoryID,CategoryName,CategoryDescription")] Category category)
        {
            if (id != category.CategoryID)
            {
                return NotFound();
            }

            var existing = await _context.Categories.FindAsync(id);
            if (existing == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = existing.OwnerID == userId;

            // Only allow edit if: Admin OR (owner AND not approved)
            if (!isAdmin && (!isOwner || existing.IsApproved == true))
            {
                _logger.LogWarning("User {UserId} attempted to edit category {CategoryId} without permission", userId, id);
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                bool nameExistOnOtherCategory = await _context.Categories
                    .AnyAsync(c => c.CategoryName.ToUpper() == category.CategoryName.ToUpper() && c.CategoryID != category.CategoryID);

                if (nameExistOnOtherCategory)
                {
                    ModelState.AddModelError(nameof(category.CategoryName), $"A Category with the name {category.CategoryName} already exists");
                }
                else
                {
                    try
                    {
                        existing.CategoryName = category.CategoryName;
                        existing.CategoryDescription = category.CategoryDescription;
                        // Reset approval status to pending (null) on any edit
                        existing.IsApproved = null;
                        
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Category {CategoryId} '{CategoryName}' updated", category.CategoryID, category.CategoryName);
                        TempData["Confirmation"] = "Modified";
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        if (!CategoryExists(category.CategoryID))
                        {
                            return NotFound();
                        }
                        else
                        {
                            _logger.LogError(ex, "Concurrency error updating category {CategoryId}", category.CategoryID);
                            throw;
                        }
                    }
                    return RedirectToAction("ConfirmationCategory", new { id = category.CategoryID });
                }
            }
            return View(category);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category with ID {CategoryId} not found for approval", id);
                return NotFound();
            }
            
            category.IsApproved = true;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Category {CategoryId} '{CategoryName}' approved", id, category.CategoryName);
            TempData["Confirmation"] = "Approved";
            return RedirectToAction(nameof(Index), new { id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category with ID {CategoryId} not found for rejection", id);
                return NotFound();
            }
            
            category.IsApproved = false;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Category {CategoryId} '{CategoryName}' rejected", id, category.CategoryName);
            TempData["Confirmation"] = "Rejected";
            return RedirectToAction(nameof(Index), new { id });
        }

        [Authorize]
        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Delete called with null id");
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.CategoryID == id);
            
            if (category == null)
            {
                _logger.LogWarning("Category with ID {CategoryId} not found for deletion", id);
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = category.OwnerID == userId;

            // Only allow delete if: Admin OR (owner AND not approved)
            if (!isAdmin && (!isOwner || category.IsApproved == true))
            {
                _logger.LogWarning("User {UserId} attempted to delete category {CategoryId} without permission", userId, id);
                TempData["Error"] = "You can only delete your own unapproved categories.";
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        [Authorize]
        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool isAdmin = User.IsInRole("Admin");
                bool isOwner = category.OwnerID == userId;

                // Only allow delete if: Admin OR (owner AND not approved)
                if (!isAdmin && (!isOwner || category.IsApproved == true))
                {
                    _logger.LogWarning("User {UserId} attempted to delete category {CategoryId} without permission", userId, id);
                    return Forbid();
                }

                // Check if any recipes use this category
                var hasRecipes = await _context.Recipes.AnyAsync(r => r.CategoryID == id);
                if (hasRecipes)
                {
                    _logger.LogWarning("Attempted to delete category {CategoryId} '{CategoryName}' but it's in use by recipes", id, category.CategoryName);
                    TempData["Error"] = $"Cannot delete category '{category.CategoryName}' because it is used by one or more recipes.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Category {CategoryId} '{CategoryName}' deleted", id, category.CategoryName);
                TempData["Confirmation"] = "Category deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult ConfirmationCategory(int id)
        {
            var category = _context.Categories.FirstOrDefault(c => c.CategoryID == id);
            if (category == null)
            {
                _logger.LogWarning("Category with ID {CategoryId} not found for confirmation page", id);
                return NotFound();
            }
            return View(category);
        }

        private bool CategoryExists(int? id)
        {
            return _context.Categories.Any(e => e.CategoryID == id);
        }

        private bool CategoryExists(string? name)
        {
            return _context.Categories.Any(e => e.CategoryName == name);
        }
    }
}
