using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using worldRecipeMvc.Models.ViewModels;
using worldRecipeMvc.Services;
using worldRecipeMvc.Services.Errors;
using System.Security.Claims;

namespace worldRecipeMvc.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const int DefaultPageSize = 15;

        private readonly IUserAdminService _userAdminService;

        public AdminController(IUserAdminService userAdminService)
        {
            _userAdminService = userAdminService;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET: Admin (user management hub)
        public async Task<IActionResult> Index(int pageNumber = 1, string? searchTerm = null)
        {
            var result = await _userAdminService.GetUsersAsync(pageNumber, DefaultPageSize, searchTerm);

            return View(new UserAdminIndexViewModel
            {
                Users = result.Value,
                SearchTerm = searchTerm,
                CurrentUserId = UserId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string id)
        {
            var result = await _userAdminService.LockAsync(id, UserId);
            SetFeedback(result.IsSuccess, "User locked.", result);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var result = await _userAdminService.UnlockAsync(id);
            SetFeedback(result.IsSuccess, "User unlocked.", result);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _userAdminService.DeleteAsync(id, UserId);
            SetFeedback(result.IsSuccess, "User deleted. Their recipes remain, without an owner.", result);
            return RedirectToAction(nameof(Index));
        }

        private void SetFeedback(bool success, string successMessage, FluentResults.Result result)
        {
            if (success)
            {
                TempData["Confirmation"] = successMessage;
            }
            else
            {
                TempData["Error"] = result.Errors.FirstOrDefault()?.Message ?? "Operation failed.";
            }
        }
    }
}
