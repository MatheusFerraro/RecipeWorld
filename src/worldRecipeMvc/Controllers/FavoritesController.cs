using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using worldRecipeMvc.Models.ViewModels;
using worldRecipeMvc.Services;
using System.Security.Claims;

namespace worldRecipeMvc.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private const int DefaultPageSize = 12;

        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET: Favorites (My Favorites page)
        public async Task<IActionResult> Index(int pageNumber = 1)
        {
            var result = await _favoriteService.GetMyFavoritesAsync(UserId, pageNumber, DefaultPageSize);
            return View(result.Value);
        }

        // POST: Favorites/Toggle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int recipeId, string? returnUrl = null)
        {
            var result = await _favoriteService.ToggleAsync(recipeId, UserId);
            if (result.IsFailed)
            {
                return NotFound();
            }

            TempData["SaveConfirmation"] = result.Value ? "Added to your favorites." : "Removed from your favorites.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Details", "Recipes", new { id = recipeId });
        }
    }
}
