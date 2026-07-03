using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using worldRecipeMvc.Services;
using worldRecipeMvc.Services.Errors;
using System.Security.Claims;

namespace worldRecipeMvc.Controllers
{
    [Authorize]
    public class RatingsController : Controller
    {
        private readonly IRatingService _ratingService;

        public RatingsController(IRatingService ratingService)
        {
            _ratingService = ratingService;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // POST: Ratings/Rate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int recipeId, int stars, string? comment)
        {
            var result = await _ratingService.UpsertAsync(recipeId, UserId, stars, comment);

            if (result.IsFailed)
            {
                if (result.HasError<NotFoundError>())
                {
                    return NotFound();
                }
                TempData["Error"] = result.Errors.First().Message;
            }
            else
            {
                TempData["SaveConfirmation"] = "Thanks for your review!";
            }

            return RedirectToAction("Details", "Recipes", new { id = recipeId });
        }

        // POST: Ratings/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int recipeId)
        {
            var result = await _ratingService.DeleteAsync(recipeId, UserId, User.IsInRole("Admin"));
            if (result.IsFailed && result.HasError<ForbiddenError>())
            {
                return Forbid();
            }

            return RedirectToAction("Details", "Recipes", new { id = recipeId });
        }
    }
}
