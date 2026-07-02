using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using worldRecipeMvc.Models;
using worldRecipeMvc.Models.ViewModels;
using worldRecipeMvc.Services;
using worldRecipeMvc.Services.Errors;
using System.Security.Claims;

namespace worldRecipeMvc.Controllers
{
    public class CategoriesController : Controller
    {
        private const int DefaultPageSize = 10;

        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User.IsInRole("Admin");

        // GET: Categories
        public async Task<IActionResult> Index(int pageNumber = 1, string? searchTerm = null, string? sortOrder = null)
        {
            ViewData["NameSortParam"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";

            var result = await _categoryService.GetCategoriesAsync(pageNumber, DefaultPageSize, searchTerm, sortOrder);

            return View(new CategoriesIndexViewModel
            {
                Categories = result.Value,
                SearchTerm = searchTerm,
                SortOrder = sortOrder
            });
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _categoryService.GetCategoryByIdAsync(id.Value);
            return result.IsFailed ? NotFound() : View(result.Value);
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
                var result = await _categoryService.CreateCategoryAsync(category, UserId!);
                if (result.IsSuccess)
                {
                    TempData["Confirmation"] = "Created";
                    return RedirectToAction("ConfirmationCategory", new { id = result.Value.CategoryID });
                }

                ModelState.AddModelError(nameof(category.CategoryName), result.Errors.First().Message);
            }
            return View(category);
        }

        [Authorize]
        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _categoryService.GetCategoryByIdAsync(id.Value);
            if (result.IsFailed)
            {
                return NotFound();
            }

            var category = result.Value;
            if (!(IsAdmin || (category.OwnerID == UserId && category.IsApproved != true)))
            {
                _logger.LogWarning("User {UserId} attempted to edit category {CategoryId} without permission", UserId, id);
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
            if (id == null || id != category.CategoryID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var result = await _categoryService.UpdateCategoryAsync(id.Value, category, UserId!, IsAdmin);
                if (result.IsSuccess)
                {
                    TempData["Confirmation"] = "Modified";
                    return RedirectToAction("ConfirmationCategory", new { id = category.CategoryID });
                }

                if (result.HasError<NotFoundError>()) return NotFound();
                if (result.HasError<ForbiddenError>()) return Forbid();
                ModelState.AddModelError(nameof(category.CategoryName), result.Errors.First().Message);
            }
            return View(category);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _categoryService.SetApprovalAsync(id, isApproved: true);
            if (result.IsFailed) return NotFound();

            TempData["Confirmation"] = "Approved";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _categoryService.SetApprovalAsync(id, isApproved: false);
            if (result.IsFailed) return NotFound();

            TempData["Confirmation"] = "Rejected";
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _categoryService.GetCategoryByIdAsync(id.Value);
            if (result.IsFailed)
            {
                return NotFound();
            }

            var category = result.Value;
            if (!(IsAdmin || (category.OwnerID == UserId && category.IsApproved != true)))
            {
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
            if (id != null)
            {
                var result = await _categoryService.DeleteCategoryAsync(id.Value, UserId!, IsAdmin);
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
                    TempData["Confirmation"] = "Category deleted successfully.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ConfirmationCategory(int id)
        {
            var result = await _categoryService.GetCategoryByIdAsync(id);
            return result.IsFailed ? NotFound() : View(result.Value);
        }
    }
}
