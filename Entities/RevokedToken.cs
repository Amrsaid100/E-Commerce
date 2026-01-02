namespace E_Commerce.Entities
{
    public class RevokedToken
    {
        public int Id { get; set; }
        public string Jti { get; set; } = default!;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime RevokedAtUtc { get; set; } = DateTime.UtcNow;
        public string? Reason { get; set; }
    }

}
