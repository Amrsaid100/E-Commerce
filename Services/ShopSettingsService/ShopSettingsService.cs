using E_Commerce.DataContext;
using E_Commerce.Dtos.Settings;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Services.ShopSettingsService
{
    public class ShopSettingsService : IShopSettingsService
    {
        private readonly EcommerceDbContext _db;

        // Whitelisted fonts
        private static readonly HashSet<string> AllowedFonts = new(StringComparer.OrdinalIgnoreCase)
        {
            "Inter", "Cairo", "Poppins", "Roboto", "Tajawal",
            "Playfair Display", "Lora", "Bebas Neue", "Pacifico", "Abril Fatface",
            "Raleway", "Montserrat", "Oswald", "Merriweather"
        };

        // Whitelisted border radii
        private static readonly HashSet<int> AllowedRadii = new() { 0, 8, 16 };

        // Whitelisted header variants
        private static readonly HashSet<string> AllowedHeaderVariants = new(StringComparer.OrdinalIgnoreCase)
        {
            "v1", "v2"
        };

        public ShopSettingsService(EcommerceDbContext db)
        {
            _db = db;
        }

        public async Task<ShopSettingsDto> GetSettingsAsync()
        {
            var entity = await GetOrCreateEntityAsync();
            return MapToDto(entity);
        }

        public async Task<ShopSettingsDto> UpdateBrandingAsync(
            string? shopName, string? logoUrl, string? faviconUrl,
            bool removeLogo, bool removeFavicon)
        {
            var entity = await GetOrCreateEntityAsync();

            if (!string.IsNullOrWhiteSpace(shopName))
                entity.ShopName = shopName.Trim();

            if (removeLogo)
                entity.LogoUrl = null;
            else if (!string.IsNullOrWhiteSpace(logoUrl))
                entity.LogoUrl = logoUrl;

            if (removeFavicon)
                entity.FaviconUrl = null;
            else if (!string.IsNullOrWhiteSpace(faviconUrl))
                entity.FaviconUrl = faviconUrl;

            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<ShopSettingsDto> UpdateThemeAsync(UpdateThemeDto dto)
        {
            var errors = new List<string>();

            if (dto.FontFamily != null && !AllowedFonts.Contains(dto.FontFamily))
                errors.Add($"FontFamily '{dto.FontFamily}' is not allowed. Allowed: {string.Join(", ", AllowedFonts)}");

            if (dto.BorderRadius.HasValue && !AllowedRadii.Contains(dto.BorderRadius.Value))
                errors.Add($"BorderRadius must be one of: {string.Join(", ", AllowedRadii)}");

            if (dto.HeaderVariant != null && !AllowedHeaderVariants.Contains(dto.HeaderVariant))
                errors.Add($"HeaderVariant must be one of: {string.Join(", ", AllowedHeaderVariants)}");

            if (errors.Count > 0)
                throw new ArgumentException(string.Join(" | ", errors));

            var entity = await GetOrCreateEntityAsync();

            if (dto.PrimaryColor != null) entity.PrimaryColor = dto.PrimaryColor;
            if (dto.SecondaryColor != null) entity.SecondaryColor = dto.SecondaryColor;
            if (dto.AccentColor != null) entity.AccentColor = dto.AccentColor;
            if (dto.BackgroundColor != null) entity.BackgroundColor = dto.BackgroundColor;
            if (dto.TextColor != null) entity.TextColor = dto.TextColor;
            if (dto.FontFamily != null) entity.FontFamily = dto.FontFamily;
            if (dto.BorderRadius.HasValue) entity.BorderRadius = dto.BorderRadius.Value;
            if (dto.HeaderVariant != null) entity.HeaderVariant = dto.HeaderVariant;

            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<ShopSettingsDto> UpdateFooterAsync(UpdateFooterDto dto)
        {
            var entity = await GetOrCreateEntityAsync();

            if (dto.FooterTagline != null) entity.FooterTagline = dto.FooterTagline;
            if (dto.WhatsApp != null) entity.WhatsApp = dto.WhatsApp;
            if (dto.FacebookUrl != null) entity.FacebookUrl = dto.FacebookUrl;
            if (dto.InstagramUrl != null) entity.InstagramUrl = dto.InstagramUrl;
            if (dto.TikTokUrl != null) entity.TikTokUrl = dto.TikTokUrl;
            if (dto.PhoneDisplay != null) entity.PhoneDisplay = dto.PhoneDisplay;
            if (dto.ContactEmail != null) entity.ContactEmail = dto.ContactEmail;

            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return MapToDto(entity);
        }

        // ── Helpers ──

        private async Task<ShopSettings> GetOrCreateEntityAsync()
        {
            var entity = await _db.ShopSettings.FirstOrDefaultAsync(s => s.Id == 1);
            if (entity == null)
            {
                entity = new ShopSettings { Id = 1 };
                _db.ShopSettings.Add(entity);
                await _db.SaveChangesAsync();
            }
            return entity;
        }

        private static ShopSettingsDto MapToDto(ShopSettings entity)
        {
            return new ShopSettingsDto
            {
                ShopName = entity.ShopName,
                LogoUrl = entity.LogoUrl,
                FaviconUrl = entity.FaviconUrl,
                PrimaryColor = entity.PrimaryColor,
                SecondaryColor = entity.SecondaryColor,
                AccentColor = entity.AccentColor,
                BackgroundColor = entity.BackgroundColor,
                TextColor = entity.TextColor,
                FontFamily = entity.FontFamily,
                BorderRadius = entity.BorderRadius,
                HeaderVariant = entity.HeaderVariant,
                FooterTagline = entity.FooterTagline,
                WhatsApp = entity.WhatsApp,
                FacebookUrl = entity.FacebookUrl,
                InstagramUrl = entity.InstagramUrl,
                TikTokUrl = entity.TikTokUrl,
                PhoneDisplay = entity.PhoneDisplay,
                ContactEmail = entity.ContactEmail,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
