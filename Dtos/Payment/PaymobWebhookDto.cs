namespace E_Commerce.Dtos.Payment
{
    public class PaymobWebhookDto
    {
        public int Id { get; set; }
        public bool Success { get; set; }
        public PaymobOrderDto? Order { get; set; }
        public string? Amount { get; set; }
        public string? Status { get; set; }
        public string? TransactionId { get; set; }
    }

    public class PaymobOrderDto
    {
        public int Id { get; set; }
        public string? Status { get; set; }
    }
}
