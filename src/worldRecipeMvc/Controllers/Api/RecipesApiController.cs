using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.DTOs;
using worldRecipeMvc.Models;
using System.Security.Claims;

namespace worldRecipeMvc.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecipesApiController> _logger;

        public RecipesApiController(ApplicationDbContext context, ILogger<RecipesApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all recipes with pagination
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <param name="search">Search term for recipe name</param>
        /// <returns>Paginated list of recipes</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<RecipeDto>), 200)]
        public async Task<ActionResult<PaginatedResponse<RecipeDto>>> GetRecipes(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Recipes
                    .Include(r => r.Category)
                    .Include(r => r.Owner)
                    .Where(r => r.Status == "Published")
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(r => r.RecipeName!.Contains(search));
                }

                var totalCount = await query.CountAsync();

                var recipes = await query
                    .OrderByDescending(r => r.RecipeID)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new RecipeDto
                    {
                        RecipeID = r.RecipeID,
                        RecipeName = r.RecipeName,
                        CategoryID = r.CategoryID,
                        CategoryName = r.Category!.CategoryName,
                        PrepTime = r.PrepTime,
                        CookTime = r.CookTime,
                        Tips = r.Tips,
                        NumberOfServings = r.NumberOfServings,
                        Status = r.Status,
                        Instructions = r.Instructions,
                        ImageUrl = r.ImageUrl,
                        Temperature = r.Temperature,
                        OwnerName = r.Owner!.UserName
                    })
                    .ToListAsync();

                var response = new PaginatedResponse<RecipeDto>
                {
                    Data = recipes,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recipes via API");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get a specific recipe by ID
        /// </summary>
        /// <param name="id">Recipe ID</param>
        /// <returns>Recipe details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RecipeDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RecipeDto>> GetRecipe(int id)
        {
            try
            {
                var recipe = await _context.Recipes
                    .Include(r => r.Category)
                    .Include(r => r.Owner)
                    .Where(r => r.RecipeID == id && r.Status == "Published")
                    .Select(r => new RecipeDto
                    {
                        RecipeID = r.RecipeID,
                        RecipeName = r.RecipeName,
                        CategoryID = r.CategoryID,
                        CategoryName = r.Category!.CategoryName,
                        PrepTime = r.PrepTime,
                        CookTime = r.CookTime,
                        Tips = r.Tips,
                        NumberOfServings = r.NumberOfServings,
                        Status = r.Status,
                        Instructions = r.Instructions,
                        ImageUrl = r.ImageUrl,
                        Temperature = r.Temperature,
                        OwnerName = r.Owner!.UserName
                    })
                    .FirstOrDefaultAsync();

                if (recipe == null)
                {
                    return NotFound();
                }

                return Ok(recipe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recipe {RecipeId} via API", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create a new recipe
        /// </summary>
        /// <param name="recipeDto">Recipe data</param>
        /// <returns>Created recipe</returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(RecipeDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<RecipeDto>> CreateRecipe([FromBody] CreateRecipeDto recipeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var recipe = new Recipe
                {
                    RecipeName = recipeDto.RecipeName,
                    CategoryID = recipeDto.CategoryID,
                    PrepTime = recipeDto.PrepTime,
                    CookTime = recipeDto.CookTime,
                    Tips = recipeDto.Tips,
                    NumberOfServings = recipeDto.NumberOfServings,
                    Instructions = recipeDto.Instructions,
                    Temperature = recipeDto.Temperature,
                    Status = "Draft",
                    OwnerID = userId,
                    ImageUrl = "/websiteImages/RecipeDefaultImage.png"
                };

                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recipe created via API: {RecipeName}", recipe.RecipeName);

                var createdRecipe = await _context.Recipes
                    .Include(r => r.Category)
                    .Include(r => r.Owner)
                    .Where(r => r.RecipeID == recipe.RecipeID)
                    .Select(r => new RecipeDto
                    {
                        RecipeID = r.RecipeID,
                        RecipeName = r.RecipeName,
                        CategoryID = r.CategoryID,
                        CategoryName = r.Category!.CategoryName,
                        PrepTime = r.PrepTime,
                        CookTime = r.CookTime,
                        Tips = r.Tips,
                        NumberOfServings = r.NumberOfServings,
                        Status = r.Status,
                        Instructions = r.Instructions,
                        ImageUrl = r.ImageUrl,
                        Temperature = r.Temperature,
                        OwnerName = r.Owner!.UserName
                    })
                    .FirstOrDefaultAsync();

                return CreatedAtAction(nameof(GetRecipe), new { id = recipe.RecipeID }, createdRecipe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating recipe via API");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update an existing recipe
        /// </summary>
        /// <param name="id">Recipe ID</param>
        /// <param name="recipeDto">Updated recipe data</param>
        /// <returns>No content</returns>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateRecipe(int id, [FromBody] UpdateRecipeDto recipeDto)
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null)
                {
                    return NotFound();
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                if (recipe.OwnerID != userId && !isAdmin)
                {
                    return Forbid();
                }

                recipe.RecipeName = recipeDto.RecipeName;
                recipe.CategoryID = recipeDto.CategoryID;
                recipe.PrepTime = recipeDto.PrepTime;
                recipe.CookTime = recipeDto.CookTime;
                recipe.Tips = recipeDto.Tips;
                recipe.NumberOfServings = recipeDto.NumberOfServings;
                recipe.Instructions = recipeDto.Instructions;
                recipe.Temperature = recipeDto.Temperature;

                if (isAdmin && !string.IsNullOrEmpty(recipeDto.Status))
                {
                    recipe.Status = recipeDto.Status;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Recipe {RecipeId} updated via API", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating recipe {RecipeId} via API", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete a recipe
        /// </summary>
        /// <param name="id">Recipe ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null)
                {
                    return NotFound();
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                if (recipe.OwnerID != userId && !isAdmin)
                {
                    return Forbid();
                }

                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recipe {RecipeId} deleted via API", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe {RecipeId} via API", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
