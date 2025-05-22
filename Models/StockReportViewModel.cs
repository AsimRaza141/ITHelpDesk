using Microsoft.AspNetCore.Mvc.Rendering;

namespace ITHelpDesk_Updated.Models
{
    public class StockReportViewModel
    {
        public List<SelectListItem>? TransactionCriteriaList { get; set; }
        public string? SelectedTransactionCriteria { get; set; }
        public List<SelectListItem>? EmployeeDetailsList { get; set; }
        public string? SelectedEmployeeDetails { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<StockReportRecord>? StockRecords { get; set; }
    }
}
