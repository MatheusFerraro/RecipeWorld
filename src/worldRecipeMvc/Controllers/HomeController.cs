using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using worldRecipeMvc.Models;
using worldRecipeMvc.Services;

namespace worldRecipeMvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRecipeService _recipeService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IRecipeService recipeService, ILogger<HomeController> logger)
        {
            _recipeService = recipeService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var trending = await _recipeService.GetTrendingAsync();
            return View(trending);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("Error page accessed with request ID: {RequestId}", Activity.Current?.Id ?? HttpContext.TraceIdentifier);
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
