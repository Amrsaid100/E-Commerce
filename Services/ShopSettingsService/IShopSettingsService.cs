using E_Commerce.Dtos.Settings;

namespace E_Commerce.Services.ShopSettingsService
{
    public interface IShopSettingsService
    {
        /// <summary>
        /// Returns current settings (creates default row if none exists).
        /// </summary>
        Task<ShopSettingsDto> GetSettingsAsync();

        /// <summary>
        /// Updates branding fields (ShopName, LogoUrl, FaviconUrl).
        /// </summary>
        Task<ShopSettingsDto> UpdateBrandingAsync(string? shopName, string? logoUrl, string? faviconUrl, bool removeLogo, bool removeFavicon);

        /// <summary>
        /// Updates theme fields (colors, font, radius, header variant).
        /// </summary>
        Task<ShopSettingsDto> UpdateThemeAsync(UpdateThemeDto dto);

        /// <summary>
        /// Updates footer fields (tagline, social links, contact info).
        /// </summary>
        Task<ShopSettingsDto> UpdateFooterAsync(UpdateFooterDto dto);
    }
}
