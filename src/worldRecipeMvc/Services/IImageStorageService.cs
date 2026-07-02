using FluentResults;

namespace worldRecipeMvc.Services
{
    public interface IImageStorageService
    {
        /// <summary>
        /// Validates and stores an uploaded recipe image.
        /// Returns the public URL ("/images/recipes/{file}") on success,
        /// or a ValidationError describing why the file was rejected.
        /// </summary>
        Task<Result<string>> SaveRecipeImageAsync(IFormFile imageFile);

        /// <summary>Deletes a previously stored recipe image; the default image is never deleted.</summary>
        void DeleteRecipeImage(string? imageUrl);
    }
}
