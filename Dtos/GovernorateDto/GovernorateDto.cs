namespace E_Commerce.Dtos.GovernorateDto
{
    public class GovernorateDto
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public decimal ShippingCost { get; set; }
    }
}
