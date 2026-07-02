using Microsoft.AspNetCore.Mvc;
using worldRecipeMvc.DTOs;
using worldRecipeMvc.Models;
using worldRecipeMvc.Services;
using System.Security.Claims;

namespace worldRecipeMvc.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientsApiController : ControllerBase
    {
        private readonly IIngredientService _ingredientService;

        public IngredientsApiController(IIngredientService ingredientService)
        {
            _ingredientService = ingredientService;
        }

        /// <summary>Get approved ingredients with pagination and optional search.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<IngredientDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetIngredients(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _ingredientService.GetIngredientsAsync(pageNumber, pageSize, search, approvedOnly: true);
            return result.Map(page => new PagedResult<IngredientDto>
            {
                Items = page.Items.Select(ToDto).ToList(),
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount
            }).ToActionResult(this);
        }

        /// <summary>Get a specific ingredient by ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IngredientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetIngredient(int id)
        {
            var result = await _ingredientService.GetIngredientByIdAsync(id);
            return result.Map(ToDto).ToActionResult(this);
        }

        /// <summary>Create a new ingredient (pending admin approval, owned by the caller).</summary>
        [HttpPost]
        [ApiAuthorize]
        [ProducesResponseType(typeof(IngredientDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> CreateIngredient([FromBody] CreateIngredientDto ingredientDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var ingredient = new Ingredient
            {
                IngredientName = ingredientDto.IngredientName,
                IngredientType = ingredientDto.IngredientType,
                IngredientDetails = ingredientDto.IngredientDetails
            };

            var result = await _ingredientService.CreateIngredientAsync(ingredient, userId);
            if (result.IsFailed)
            {
                return result.ToResult().ToActionResult(this);
            }

            var created = result.Value;
            return CreatedAtAction(nameof(GetIngredient), new { id = created.IngredientID }, ToDto(created));
        }

        private static IngredientDto ToDto(Ingredient i) => new()
        {
            IngredientID = i.IngredientID,
            IngredientName = i.IngredientName,
            IngredientType = i.IngredientType,
            IngredientDetails = i.IngredientDetails,
            IsApproved = i.IsApproved
        };
    }
}
