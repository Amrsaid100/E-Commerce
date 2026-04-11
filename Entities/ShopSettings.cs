using System;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Entities
{
    /// <summary>
    /// Single-row table (Id = 1 always) holding shop branding & theme configuration.
    /// </summary>
    public class ShopSettings
    {
        [Key]
        public int Id { get; set; } = 1;

        // ── Branding ──
        [MaxLength(200)]
        public string ShopName { get; set; } = "FREE ONE";

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(500)]
        public string? FaviconUrl { get; set; }

        // ── Theme Colors (hex #RRGGBB) ──
        [MaxLength(7)]
        public string PrimaryColor { get; set; } = "#0B0B0B";

        [MaxLength(7)]
        public string SecondaryColor { get; set; } = "#FAFAFA";

        [MaxLength(7)]
        public string AccentColor { get; set; } = "#EF4444";

        [MaxLength(7)]
        public string BackgroundColor { get; set; } = "#FAFAFA";

        [MaxLength(7)]
        public string TextColor { get; set; } = "#0B0B0B";

        // ── Typography ──
        [MaxLength(50)]
        public string FontFamily { get; set; } = "Inter";

        // ── Shape ──
        public int BorderRadius { get; set; } = 8; // 0, 8, or 16

        // ── Layout ──
        [MaxLength(10)]
        public string HeaderVariant { get; set; } = "v1"; // "v1" or "v2"

        // ── Footer ──
        [MaxLength(200)] 

        public string FooterTagline { get; set; } = "Your premium shopping destination";

        [MaxLength(20)]
        public string? WhatsApp { get; set; } = "201011944466";

        [MaxLength(500)]
        public string? FacebookUrl { get; set; }

        [MaxLength(500)]
        public string? InstagramUrl { get; set; }

        [MaxLength(500)]
        public string? TikTokUrl { get; set; }

        [MaxLength(100)]
        public string? PhoneDisplay { get; set; } = "+20 101 1944466";

        [MaxLength(100)]
        public string? ContactEmail { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
