namespace ITHelpDesk_Updated.Models
{
    public class Transaction
    {
        public string TransactionCode { get; set; }
        public string VendorName { get; set; }
        public string ItemCode { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string ItemCategory { get; set; }
        public string ItemStorage { get; set; }
        public DateTime DateOfPurchase { get; set; }
        public decimal Price { get; set; }
        public int ReceivedQuantity { get; set; }
    }
}
