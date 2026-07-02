using FluentResults;
using worldRecipeMvc.Services.Errors;

namespace worldRecipeMvc.Services
{
    public class ImageStorageService : IImageStorageService
    {
        public const string DefaultImageUrl = "/WebsiteImages/RecipeDefaultImageNotAvailable.png";
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        // File signatures (magic bytes) per allowed extension. A file whose leading
        // bytes don't match its extension is rejected even if the extension is allowed.
        private static readonly Dictionary<string, List<byte[]>> FileSignatures = new()
        {
            [".jpg"] = new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".png"] = new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".gif"] = new List<byte[]> { "GIF87a"u8.ToArray(), "GIF89a"u8.ToArray() },
            // RIFF....WEBP — bytes 4-7 are the file size, checked separately below
            [".webp"] = new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } }
        };

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImageStorageService> _logger;

        public ImageStorageService(IWebHostEnvironment environment, IConfiguration configuration, ILogger<ImageStorageService> logger)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Result<string>> SaveRecipeImageAsync(IFormFile imageFile)
        {
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("Rejected image upload with extension {Extension}", extension);
                return Result.Fail(new ValidationError("Only image files (jpg, jpeg, png, gif, webp) are allowed.", "ImageFile"));
            }

            if (imageFile.Length == 0 || imageFile.Length > MaxFileSizeBytes)
            {
                _logger.LogWarning("Rejected image upload of {Size} bytes", imageFile.Length);
                return Result.Fail(new ValidationError("Image file size must be between 1 byte and 5MB.", "ImageFile"));
            }

            if (!await MatchesSignatureAsync(imageFile, extension))
            {
                _logger.LogWarning("Rejected image upload: content does not match {Extension} signature", extension);
                return Result.Fail(new ValidationError("The file content does not match its extension. Upload a real image file.", "ImageFile"));
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = GetUploadsFolder();
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            _logger.LogInformation("Recipe image saved: {FileName}", uniqueFileName);
            return Result.Ok($"/images/recipes/{uniqueFileName}");
        }

        public void DeleteRecipeImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || imageUrl.Contains("RecipeDefaultImage"))
            {
                return;
            }

            try
            {
                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(GetUploadsFolder(), fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted old recipe image: {FileName}", fileName);
                }
            }
            catch (IOException ex)
            {
                // A leftover file is not worth failing the recipe operation for
                _logger.LogError(ex, "Error deleting recipe image: {ImageUrl}", imageUrl);
            }
        }

        private string GetUploadsFolder()
        {
            var configured = _configuration["ImageUpload:StoragePath"];
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(_environment.WebRootPath, "images", "recipes")
                : configured;
        }

        private static async Task<bool> MatchesSignatureAsync(IFormFile imageFile, string extension)
        {
            var signatures = FileSignatures[extension];
            var headerLength = extension == ".webp" ? 12 : signatures.Max(s => s.Length);

            var header = new byte[headerLength];
            await using var stream = imageFile.OpenReadStream();
            var read = await stream.ReadAtLeastAsync(header, headerLength, throwOnEndOfStream: false);
            if (read < headerLength)
            {
                return false;
            }

            if (extension == ".webp")
            {
                return header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
            }

            return signatures.Any(signature => header.AsSpan(0, signature.Length).SequenceEqual(signature));
        }
    }
}
