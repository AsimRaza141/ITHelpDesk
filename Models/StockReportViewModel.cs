using Microsoft.AspNetCore.Mvc.Rendering;

namespace ITHelpDesk_Updated.Models
{
    public class StockReportViewModel
    {
        public string? SelectedTransactionCriteria { get; set; }
        public List<SelectListItem>? TransactionCriteriaList { get; set; }
        public List<StockReportRecord>? StockRecords { get; set; }
    }
}
