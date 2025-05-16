namespace ITHelpDesk_Updated.Models
{
    public class StockReportRecord
    {
        public string TransactionCode { get; set; }
        public string ItemName { get; set; }
        public int ReceivedQuantity { get; set; }
        public int IssuedQuantity { get; set; }
        public int Stock { get; set; }
    }
}
