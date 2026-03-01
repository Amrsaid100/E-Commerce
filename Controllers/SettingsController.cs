using E_Commerce.Dtos.Settings;
using E_Commerce.Services.FileStorage;
using E_Commerce.Services.ShopSettingsService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : ControllerBase
    {
        private readonly IShopSettingsService _settingsService;
        private readonly IFileStorageService _fileStorage;

        public SettingsController(IShopSettingsService settingsService, IFileStorageService fileStorage)
        {
            _settingsService = settingsService;
            _fileStorage = fileStorage;
        }

        // ───────────── GET /api/settings ─────────────
        // Public endpoint — returns branding + theme (used by storefront at boot)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetSettings()
        {
            var dto = await _settingsService.GetSettingsAsync();
            return Ok(dto);
        }

        // ───────────── PUT /api/settings/branding ─────────────
        // Owner-only. Accepts multipart/form-data with optional logo & favicon files.
        [HttpPut("branding")]
        [Authorize(Roles = "Owner")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateBranding(
            [FromForm] string? shopName,
            [FromForm] bool removeLogo,
            [FromForm] bool removeFavicon,
            IFormFile? logo,
            IFormFile? favicon)
        {
            try
            {
                string? logoUrl = null;
                string? faviconUrl = null;

                if (logo != null)
                    logoUrl = await _fileStorage.SaveFileAsync(logo, "branding");

                if (favicon != null)
                    faviconUrl = await _fileStorage.SaveFileAsync(favicon, "branding");

                var result = await _settingsService.UpdateBrandingAsync(shopName, logoUrl, faviconUrl, removeLogo, removeFavicon);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ───────────── PUT /api/settings/theme ─────────────
        // Owner-only. JSON body with color/font/radius/headerVariant fields.
        [HttpPut("theme")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateTheme([FromBody] UpdateThemeDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                var result = await _settingsService.UpdateThemeAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ───────────── PUT /api/settings/footer ─────────────
        // Owner-only. JSON body with footer fields.
        [HttpPut("footer")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateFooter([FromBody] UpdateFooterDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                var result = await _settingsService.UpdateFooterAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
