namespace E_Commerce.Entities
{
    public class Governorate
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public decimal ShippingCost { get; set; }
    }
}
