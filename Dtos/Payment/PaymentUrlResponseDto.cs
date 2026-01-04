namespace E_Commerce.Dtos.Payment
{
    public class PaymentUrlResponseDto
    {
        public string? PaymentUrl { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
