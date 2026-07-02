using Microsoft.AspNetCore.Mvc;
using worldRecipeMvc.DTOs;
using worldRecipeMvc.Services;

namespace worldRecipeMvc.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesApiController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesApiController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>Get all approved categories.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CategoryDto>>> GetCategories()
        {
            var categories = await _categoryService.GetApprovedCategoriesAsync();

            return Ok(categories.Select(c => new CategoryDto
            {
                CategoryID = c.CategoryID,
                CategoryName = c.CategoryName,
                CategoryDescription = c.CategoryDescription,
                IsApproved = c.IsApproved
            }).ToList());
        }

        /// <summary>Get a specific category by ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCategory(int id)
        {
            var result = await _categoryService.GetCategoryByIdAsync(id);
            return result.Map(c => new CategoryDto
            {
                CategoryID = c.CategoryID,
                CategoryName = c.CategoryName,
                CategoryDescription = c.CategoryDescription,
                IsApproved = c.IsApproved
            }).ToActionResult(this);
        }
    }
}
