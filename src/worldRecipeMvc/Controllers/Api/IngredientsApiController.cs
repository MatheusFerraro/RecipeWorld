using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using worldRecipeMvc.Data;
using worldRecipeMvc.DTOs;
using worldRecipeMvc.Models;

namespace worldRecipeMvc.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IngredientsApiController> _logger;

        public IngredientsApiController(ApplicationDbContext context, ILogger<IngredientsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all approved ingredients with pagination
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<IngredientDto>), 200)]
        public async Task<ActionResult<PaginatedResponse<IngredientDto>>> GetIngredients(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Ingredients.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(i => i.IngredientName!.Contains(search));
                }

                var totalCount = await query.CountAsync();

                var ingredients = await query
                    .OrderBy(i => i.IngredientName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(i => new IngredientDto
                    {
                        IngredientID = i.IngredientID,
                        IngredientName = i.IngredientName,
                        IngredientType = i.IngredientType,
                        IngredientDetails = i.IngredientDetails,
                        IsApproved = i.IsApproved
                    })
                    .ToListAsync();

                var response = new PaginatedResponse<IngredientDto>
                {
                    Data = ingredients,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ingredients via API");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get a specific ingredient by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IngredientDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IngredientDto>> GetIngredient(int id)
        {
            try
            {
                var ingredient = await _context.Ingredients
                    .Where(i => i.IngredientID == id)
                    .Select(i => new IngredientDto
                    {
                        IngredientID = i.IngredientID,
                        IngredientName = i.IngredientName,
                        IngredientType = i.IngredientType,
                        IngredientDetails = i.IngredientDetails,
                        IsApproved = i.IsApproved
                    })
                    .FirstOrDefaultAsync();

                if (ingredient == null)
                {
                    return NotFound();
                }

                return Ok(ingredient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ingredient {IngredientId} via API", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create a new ingredient (requires authentication)
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(IngredientDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IngredientDto>> CreateIngredient([FromBody] IngredientDto ingredientDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var ingredient = new Ingredient
                {
                    IngredientName = ingredientDto.IngredientName,
                    IngredientType = ingredientDto.IngredientType,
                    IngredientDetails = ingredientDto.IngredientDetails,
                    IsApproved = null
                };

                _context.Ingredients.Add(ingredient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ingredient created via API: {IngredientName}", ingredient.IngredientName);

                var createdIngredient = new IngredientDto
                {
                    IngredientID = ingredient.IngredientID,
                    IngredientName = ingredient.IngredientName,
                    IngredientType = ingredient.IngredientType,
                    IngredientDetails = ingredient.IngredientDetails,
                    IsApproved = ingredient.IsApproved
                };

                return CreatedAtAction(nameof(GetIngredient), new { id = ingredient.IngredientID }, createdIngredient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ingredient via API");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
