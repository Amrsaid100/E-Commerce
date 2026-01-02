namespace E_Commerce.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }              
        public string TokenHash { get; set; } = default!;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAtUtc { get; set; }
        public string? ReplacedByTokenHash { get; set; }

        public bool IsRevoked => RevokedAtUtc != null;
        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    }

}
