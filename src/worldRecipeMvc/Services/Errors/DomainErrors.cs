using FluentResults;

namespace worldRecipeMvc.Services.Errors
{
    /// <summary>Requested entity does not exist (maps to HTTP 404).</summary>
    public class NotFoundError : Error
    {
        public NotFoundError(string entityName, object key)
            : base($"{entityName} with id '{key}' was not found.")
        {
        }
    }

    /// <summary>Caller is not allowed to perform the operation (maps to HTTP 403).</summary>
    public class ForbiddenError : Error
    {
        public ForbiddenError(string message = "You are not allowed to perform this operation.")
            : base(message)
        {
        }
    }

    /// <summary>Input failed a business validation rule (maps to HTTP 400).</summary>
    public class ValidationError : Error
    {
        public string? PropertyName { get; }

        public ValidationError(string message, string? propertyName = null)
            : base(message)
        {
            PropertyName = propertyName;
        }
    }

    /// <summary>Operation conflicts with existing state, e.g. duplicate name (maps to HTTP 409).</summary>
    public class ConflictError : Error
    {
        public ConflictError(string message)
            : base(message)
        {
        }
    }
}
