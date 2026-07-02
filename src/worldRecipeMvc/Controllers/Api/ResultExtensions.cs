using FluentResults;
using Microsoft.AspNetCore.Mvc;
using worldRecipeMvc.Services.Errors;

namespace worldRecipeMvc.Controllers.Api
{
    /// <summary>
    /// Maps FluentResults results from the service layer to HTTP responses:
    /// NotFoundError → 404, ForbiddenError → 403, ValidationError → 400,
    /// ConflictError → 409, anything else → 500. Failure bodies are ProblemDetails.
    /// </summary>
    public static class ResultExtensions
    {
        public static ActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
        {
            return result.IsSuccess
                ? controller.Ok(result.Value)
                : MapFailure(result.ToResult(), controller);
        }

        public static ActionResult ToActionResult(this Result result, ControllerBase controller)
        {
            return result.IsSuccess
                ? controller.NoContent()
                : MapFailure(result, controller);
        }

        private static ActionResult MapFailure(Result result, ControllerBase controller)
        {
            var detail = result.Errors.FirstOrDefault()?.Message ?? "An error occurred.";

            if (result.HasError<NotFoundError>())
            {
                return controller.NotFound(Problem(StatusCodes.Status404NotFound, "Not Found", detail));
            }

            if (result.HasError<ForbiddenError>())
            {
                return new ObjectResult(Problem(StatusCodes.Status403Forbidden, "Forbidden", detail))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            if (result.HasError<ValidationError>())
            {
                return controller.BadRequest(Problem(StatusCodes.Status400BadRequest, "Validation Failed", detail));
            }

            if (result.HasError<ConflictError>())
            {
                return controller.Conflict(Problem(StatusCodes.Status409Conflict, "Conflict", detail));
            }

            return new ObjectResult(Problem(StatusCodes.Status500InternalServerError, "Server Error", "An error occurred while processing your request."))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        private static ProblemDetails Problem(int status, string title, string detail) =>
            new() { Status = status, Title = title, Detail = detail };
    }
}
