using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Dtos.Settings
{
    // ── Response DTO ──
    public class ShopSettingsDto
    {
        // Branding
        public string ShopName { get; set; } = "FREE ONE";
        public string? LogoUrl { get; set; }
        public string? FaviconUrl { get; set; }

        // Theme
        public string PrimaryColor { get; set; } = "#0B0B0B";
        public string SecondaryColor { get; set; } = "#FAFAFA";
        public string AccentColor { get; set; } = "#EF4444";
        public string BackgroundColor { get; set; } = "#FAFAFA";
        public string TextColor { get; set; } = "#0B0B0B";
        public string FontFamily { get; set; } = "Inter";
        public int BorderRadius { get; set; } = 8;
        public string HeaderVariant { get; set; } = "v1";

        // Footer
        public string FooterTagline { get; set; } = "Your premium shopping destination";
        public string? WhatsApp { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? TikTokUrl { get; set; }
        public string? PhoneDisplay { get; set; }
        public string? ContactEmail { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    // ── Branding update (multipart/form-data) ──
    public class UpdateBrandingDto
    {
        [MaxLength(200)]
        public string? ShopName { get; set; }

        // Files are handled via IFormFile in the controller
    }

    // ── Footer update (JSON body) ──
    public class UpdateFooterDto
    {
        [MaxLength(300)]
        public string? FooterTagline { get; set; }

        /// <summary>Required — WhatsApp number digits only, e.g. 201011944466</summary>
        [MaxLength(20)]
        [RegularExpression(@"^\d{7,20}$", ErrorMessage = "WhatsApp must be digits only, 7-20 characters.")]
        public string? WhatsApp { get; set; }

        /// <summary>Required — Display text for phone, e.g. +20 101 1944466</summary>
        [MaxLength(100)]
        public string? PhoneDisplay { get; set; }

        // Optional social links
        [MaxLength(500)]
        public string? FacebookUrl { get; set; }

        [MaxLength(500)]
        public string? InstagramUrl { get; set; }

        [MaxLength(500)]
        public string? TikTokUrl { get; set; }

        // Optional email
        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "ContactEmail must be a valid email.")]
        public string? ContactEmail { get; set; }
    }

    // ── Theme update (JSON body) ──
    public class UpdateThemeDto
    {
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "PrimaryColor must be a valid hex color (#RRGGBB).")]
        public string? PrimaryColor { get; set; }

        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "SecondaryColor must be a valid hex color (#RRGGBB).")]
        public string? SecondaryColor { get; set; }

        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "AccentColor must be a valid hex color (#RRGGBB).")]
        public string? AccentColor { get; set; }

        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "BackgroundColor must be a valid hex color (#RRGGBB).")]
        public string? BackgroundColor { get; set; }

        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "TextColor must be a valid hex color (#RRGGBB).")]
        public string? TextColor { get; set; }

        /// <summary>Allowed: Inter, Cairo, Poppins, Roboto, Tajawal</summary>
        public string? FontFamily { get; set; }

        /// <summary>Allowed: 0, 8, 16</summary>
        public int? BorderRadius { get; set; }

        /// <summary>Allowed: v1, v2</summary>
        public string? HeaderVariant { get; set; }
    }
}
