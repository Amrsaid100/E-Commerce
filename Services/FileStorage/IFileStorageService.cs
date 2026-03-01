using Microsoft.AspNetCore.Http;

namespace E_Commerce.Services.FileStorage
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves an uploaded file to wwwroot/uploads/{subfolder}.
        /// Returns the public URL path (e.g. /uploads/branding/logo_abc123.png).
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file, string subfolder);

        /// <summary>
        /// Deletes a previously-saved file by its public URL path.
        /// No-op if file doesn't exist.
        /// </summary>
        void DeleteFile(string? publicUrl);
    }
}
