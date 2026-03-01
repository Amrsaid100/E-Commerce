using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Services.FileStorage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LocalFileStorageService> _logger;

        // Allowed MIME types for image uploads
        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/png",
            "image/jpeg",
            "image/jpg",
            "image/svg+xml"
        };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".svg"
        };

        private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

        public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subfolder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException($"File exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");

            // Validate MIME type
            if (!AllowedMimeTypes.Contains(file.ContentType))
                throw new ArgumentException($"File type '{file.ContentType}' is not allowed. Allowed: png, jpg, jpeg, svg.");

            // Validate extension
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
                throw new ArgumentException($"File extension '{ext}' is not allowed.");

            // Build safe file name: sanitize and add unique suffix
            var safeName = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
            var uniqueName = $"{safeName}_{Guid.NewGuid():N}{ext.ToLowerInvariant()}";

            // Ensure target directory exists
            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", subfolder);
            Directory.CreateDirectory(uploadsDir);

            var filePath = Path.Combine(uploadsDir, uniqueName);

            // Block path traversal
            var fullTargetDir = Path.GetFullPath(uploadsDir);
            var fullFilePath = Path.GetFullPath(filePath);
            if (!fullFilePath.StartsWith(fullTargetDir, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid file path detected.");

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            _logger.LogInformation("File saved: {FilePath}", filePath);

            // Return public URL path
            return $"/uploads/{subfolder}/{uniqueName}";
        }

        public void DeleteFile(string? publicUrl)
        {
            if (string.IsNullOrWhiteSpace(publicUrl))
                return;

            try
            {
                // publicUrl looks like: /uploads/branding/logo_abc.png
                var relativePath = publicUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("File deleted: {FilePath}", fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file: {Url}", publicUrl);
            }
        }

        /// <summary>
        /// Strips dangerous characters from file names.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "file";

            // Remove path separators and other dangerous chars
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Where(c => !invalid.Contains(c) && c != '.' && c != ' ').ToArray());

            return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
        }
    }
}
