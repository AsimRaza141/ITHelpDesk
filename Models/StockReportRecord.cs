namespace ITHelpDesk_Updated.Models
{
    public class StockReportRecord
    {
        public string TransactionCode { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string IssueTo { get; set; }
        public string EmployeeDetails { get; set; }
        public DateTime? DateOfIssue { get; set; }
        public int ReceivedQuantity { get; set; }
        public int IssuedQuantity { get; set; }
        public int Stock { get; set; }
        public string ItemCategory { get; set; }
    }
}
