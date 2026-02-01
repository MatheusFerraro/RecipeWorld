using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.DTOs;

namespace worldRecipeMvc.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesApiController> _logger;

        public CategoriesApiController(ApplicationDbContext context, ILogger<CategoriesApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all approved categories
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryDto>), 200)]
        public async Task<ActionResult<List<CategoryDto>>> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsApproved == true)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new CategoryDto
                    {
                        CategoryID = c.CategoryID,
                        CategoryName = c.CategoryName,
                        CategoryDescription = c.CategoryDescription,
                        IsApproved = c.IsApproved
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories via API");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get a specific category by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CategoryDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Where(c => c.CategoryID == id)
                    .Select(c => new CategoryDto
                    {
                        CategoryID = c.CategoryID,
                        CategoryName = c.CategoryName,
                        CategoryDescription = c.CategoryDescription,
                        IsApproved = c.IsApproved
                    })
                    .FirstOrDefaultAsync();

                if (category == null)
                {
                    return NotFound();
                }

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category {CategoryId} via API", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
