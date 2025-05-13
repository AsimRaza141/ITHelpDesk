namespace ITHelpDesk_Updated.Models
{
    public class TransactionIssuance
    {
        public string IssueTo { get; set; }
        public string EmployeeDetails { get; set; }
        public string TransactionCode { get; set; }
        public string Description { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string ItemCategory { get; set; }
        public string ItemStorage { get; set; }
        public DateTime DateOfIssue { get; set; }
        public int IssuedQuantity { get; set; }
        public int Stock { get; set; }
    }
}
