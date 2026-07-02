using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using worldRecipeMvc.DTOs;
using worldRecipeMvc.Models;
using worldRecipeMvc.Services;
using System.Security.Claims;

namespace worldRecipeMvc.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipesApiController : ControllerBase
    {
        private readonly IRecipeService _recipeService;
        private readonly IRatingService _ratingService;
        private readonly IFavoriteService _favoriteService;

        public RecipesApiController(IRecipeService recipeService, IRatingService ratingService, IFavoriteService favoriteService)
        {
            _recipeService = recipeService;
            _ratingService = ratingService;
            _favoriteService = favoriteService;
        }

        /// <summary>Get public recipes with pagination and optional name search.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<RecipeDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetRecipes(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _recipeService.GetPublicRecipesAsync(pageNumber, pageSize, search);
            return result.ToActionResult(this);
        }

        /// <summary>Get a specific public recipe by ID.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RecipeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetRecipe(int id)
        {
            var result = await _recipeService.GetPublicRecipeAsync(id);
            return result.ToActionResult(this);
        }

        /// <summary>Create a new recipe (created as Draft, owned by the caller).</summary>
        [HttpPost]
        [ApiAuthorize]
        [ProducesResponseType(typeof(RecipeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> CreateRecipe([FromBody] CreateRecipeDto recipeDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var recipe = new Recipe
            {
                RecipeName = recipeDto.RecipeName,
                CategoryID = recipeDto.CategoryID,
                PrepTime = recipeDto.PrepTime,
                CookTime = recipeDto.CookTime,
                Tips = recipeDto.Tips,
                NumberOfServings = recipeDto.NumberOfServings,
                Instructions = recipeDto.Instructions,
                Temperature = recipeDto.Temperature
            };

            var result = await _recipeService.CreateRecipeAsync(recipe, Enumerable.Empty<RecipeIngredient>(), userId);
            if (result.IsFailed)
            {
                return result.ToResult().ToActionResult(this);
            }

            var created = result.Value;
            var dto = new RecipeDto
            {
                RecipeID = created.RecipeID,
                RecipeName = created.RecipeName,
                CategoryID = created.CategoryID,
                PrepTime = created.PrepTime,
                CookTime = created.CookTime,
                Tips = created.Tips,
                NumberOfServings = created.NumberOfServings,
                Status = created.Status,
                Instructions = created.Instructions,
                ImageUrl = created.ImageUrl,
                Temperature = created.Temperature
            };

            return CreatedAtAction(nameof(GetRecipe), new { id = created.RecipeID }, dto);
        }

        /// <summary>Update an existing recipe (owner or admin).</summary>
        [HttpPut("{id}")]
        [ApiAuthorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> UpdateRecipe(int id, [FromBody] UpdateRecipeDto recipeDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");

            var input = new Recipe
            {
                RecipeName = recipeDto.RecipeName,
                CategoryID = recipeDto.CategoryID,
                PrepTime = recipeDto.PrepTime,
                CookTime = recipeDto.CookTime,
                Tips = recipeDto.Tips,
                NumberOfServings = recipeDto.NumberOfServings,
                Instructions = recipeDto.Instructions,
                Temperature = recipeDto.Temperature,
                Status = recipeDto.Status
            };

            var result = await _recipeService.UpdateRecipeAsync(id, input, ingredients: null, userId, isAdmin);
            return result.ToActionResult(this);
        }

        /// <summary>Delete a recipe (owner or admin).</summary>
        [HttpDelete("{id}")]
        [ApiAuthorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteRecipe(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");

            var result = await _recipeService.DeleteRecipeAsync(id, userId, isAdmin);
            return result.ToActionResult(this);
        }

        /// <summary>Get a recipe's reviews (paged, newest first).</summary>
        [HttpGet("{id}/ratings")]
        [ProducesResponseType(typeof(PagedResult<RatingDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetRatings(int id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _ratingService.GetReviewsAsync(id, pageNumber, pageSize);
            return result.Map(page => new PagedResult<RatingDto>
            {
                Items = page.Items.Select(r => new RatingDto
                {
                    RecipeID = r.RecipeID,
                    Stars = r.Stars,
                    Comment = r.Comment,
                    ReviewerName = r.User?.FullName ?? r.User?.UserName,
                    CreatedAtUtc = r.CreatedAtUtc
                }).ToList(),
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount
            }).ToActionResult(this);
        }

        /// <summary>Rate a recipe 1-5 stars with an optional comment (one rating per user).</summary>
        [HttpPost("{id}/ratings")]
        [ApiAuthorize]
        [ProducesResponseType(typeof(RatingSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RateRecipe(int id, [FromBody] CreateRatingDto ratingDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _ratingService.UpsertAsync(id, userId, ratingDto.Stars, ratingDto.Comment);
            if (result.IsFailed)
            {
                return result.ToResult().ToActionResult(this);
            }

            var summary = await _ratingService.GetSummaryAsync(id, userId);
            return Ok(new RatingSummaryDto
            {
                RecipeID = id,
                AverageStars = summary.AverageStars,
                Count = summary.Count,
                YourStars = summary.CurrentUserRating?.Stars
            });
        }

        /// <summary>Toggle the recipe in the caller's favorites; returns the new state.</summary>
        [HttpPost("{id}/favorite")]
        [ApiAuthorize]
        [ProducesResponseType(typeof(FavoriteStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ToggleFavorite(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _favoriteService.ToggleAsync(id, userId);
            if (result.IsFailed)
            {
                return result.ToResult().ToActionResult(this);
            }

            var info = await _favoriteService.GetInfoAsync(id, userId);
            return Ok(new FavoriteStatusDto
            {
                RecipeID = id,
                IsFavorited = result.Value,
                FavoriteCount = info.Count
            });
        }
    }
}
