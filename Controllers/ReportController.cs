using ClosedXML.Excel;
using ITHelpDesk_Updated.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace ITHelpDesk_Updated.Controllers
{
    [NoCache]
    public class ReportController : Controller
    {
        private readonly string connectionString = "Data Source=192.168.0.75;Initial Catalog=ITHD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

        [HttpGet]
        public IActionResult CreateStockReport()
        {
            if (HttpContext.Session.GetString("USR_NAME") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new StockReportViewModel
            {
                TransactionCriteriaList = GetTransactionCriteria(),
                EmployeeDetailsList = GetEmployeeDetailsList(),
                StockRecords = GetStockRecords(null, null, null, null) // Load all records by default
            };

            return View("~/Views/ITHelpDesk/CreateStockReport.cshtml", model);
        }

        [HttpPost]
        public IActionResult CreateStockReport(StockReportViewModel model)
        {
            if (HttpContext.Session.GetString("USR_NAME") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.StockRecords = GetStockRecords(
                model.SelectedTransactionCriteria,
                model.SelectedEmployeeDetails,
                model.StartDate,
                model.EndDate
            );
            model.TransactionCriteriaList = GetTransactionCriteria();
            model.EmployeeDetailsList = GetEmployeeDetailsList();
            return View("~/Views/ITHelpDesk/CreateStockReport.cshtml", model);
        }

        [HttpPost]
        public IActionResult GenerateReport(StockReportViewModel model)
        {
            if (HttpContext.Session.GetString("USR_NAME") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var stockRecords = GetStockRecords(
                model.SelectedTransactionCriteria,
                model.SelectedEmployeeDetails,
                model.StartDate,
                model.EndDate
            );

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Stock Ledger");

                // Add headers
                worksheet.Cell(1, 1).Value = "Transaction Code";
                worksheet.Cell(1, 2).Value = "Item Name";
                worksheet.Cell(1, 3).Value = "Item Category";
                worksheet.Cell(1, 4).Value = "Description";
                worksheet.Cell(1, 5).Value = "Issue To";
                worksheet.Cell(1, 6).Value = "Employee Details";
                worksheet.Cell(1, 7).Value = "Date of Issue";
                worksheet.Cell(1, 8).Value = "Stock-In";
                worksheet.Cell(1, 9).Value = "Stock-Out";
                worksheet.Cell(1, 10).Value = "Stock";

                // Style headers
                var headerRange = worksheet.Range("A1:J1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Add data
                for (int i = 0; i < stockRecords.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = stockRecords[i].TransactionCode;
                    worksheet.Cell(i + 2, 2).Value = stockRecords[i].ItemName;
                    worksheet.Cell(i + 2, 3).Value = stockRecords[i].ItemCategory;
                    worksheet.Cell(i + 2, 4).Value = stockRecords[i].Description;
                    worksheet.Cell(i + 2, 5).Value = stockRecords[i].IssueTo;
                    worksheet.Cell(i + 2, 6).Value = stockRecords[i].EmployeeDetails;
                    worksheet.Cell(i + 2, 7).Value = stockRecords[i].DateOfIssue?.ToString("yyyy-MM-dd") ?? "";
                    worksheet.Cell(i + 2, 8).Value = stockRecords[i].ReceivedQuantity;
                    worksheet.Cell(i + 2, 9).Value = stockRecords[i].IssuedQuantity;
                    worksheet.Cell(i + 2, 10).Value = stockRecords[i].Stock;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Save to stream
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StockLedgerReport.xlsx");
                }
            }
        }

        private List<SelectListItem> GetTransactionCriteria()
        {
            var criteriaList = new List<SelectListItem>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                SELECT DISTINCT ti.TransactionCode, ti.ItemName
                FROM TransactionIssuance ti
                ORDER BY ti.TransactionCode, ti.ItemName";
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string transactionCode = reader["TransactionCode"] as string;
                    string itemName = reader["ItemName"] as string;
                    string value = $"{transactionCode}|{itemName}";
                    criteriaList.Add(new SelectListItem
                    {
                        Value = value,
                        Text = $"{transactionCode} - {itemName}"
                    });
                }
            }
            return criteriaList;
        }

        private List<SelectListItem> GetEmployeeDetailsList()
        {
            var employeeDetailsList = new List<SelectListItem>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                SELECT DISTINCT ti.EmployeeDetails
                FROM TransactionIssuance ti
                WHERE ti.EmployeeDetails IS NOT NULL
                ORDER BY ti.EmployeeDetails";
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string employeeDetails = reader["EmployeeDetails"] as string;
                    employeeDetailsList.Add(new SelectListItem
                    {
                        Value = employeeDetails,
                        Text = employeeDetails
                    });
                }
            }
            return employeeDetailsList;
        }

        private List<StockReportRecord> GetStockRecords(string selectedCriteria, string selectedEmployeeDetails, DateTime? startDate, DateTime? endDate)
        {
            var stockRecords = new List<StockReportRecord>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                SELECT 
                    ti.TransactionCode,
                    ti.ItemName,
                    ti.ItemCategory,
                    ti.Description,
                    ti.IssueTo,
                    ti.EmployeeDetails,
                    ti.DateOfIssue,
                    t.ReceivedQuantity,
                    ti.IssuedQuantity,
                    ti.Stock
                FROM TransactionIssuance ti
                LEFT JOIN Transactions t ON ti.TransactionCode = t.TransactionCode
                WHERE 1=1";

                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(selectedCriteria))
                {
                    var parts = selectedCriteria.Split('|');
                    if (parts.Length == 2)
                    {
                        query += " AND ti.TransactionCode = @TransactionCode AND ti.ItemName = @ItemName";
                        parameters.Add(new SqlParameter("@TransactionCode", parts[0]));
                        parameters.Add(new SqlParameter("@ItemName", parts[1]));
                    }
                }

                if (!string.IsNullOrEmpty(selectedEmployeeDetails))
                {
                    query += " AND ti.EmployeeDetails = @EmployeeDetails";
                    parameters.Add(new SqlParameter("@EmployeeDetails", selectedEmployeeDetails));
                }

                if (startDate.HasValue && endDate.HasValue)
                {
                    query += " AND ti.DateOfIssue BETWEEN @StartDate AND @EndDate ORDER BY DateOfIssue ASC";
                    parameters.Add(new SqlParameter("@StartDate", startDate.Value));
                    parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddTicks(-1)));
                }

                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddRange(parameters.ToArray());

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    stockRecords.Add(new StockReportRecord
                    {
                        TransactionCode = reader["TransactionCode"] as string,
                        ItemName = reader["ItemName"] as string,
                        ItemCategory = reader["ItemCategory"] as string,
                        Description = reader["Description"] as string,
                        IssueTo = reader["IssueTo"] as string,
                        EmployeeDetails = reader["EmployeeDetails"] as string,
                        DateOfIssue = reader["DateOfIssue"] as DateTime?,
                        ReceivedQuantity = reader["ReceivedQuantity"] as int? ?? 0,
                        IssuedQuantity = reader["IssuedQuantity"] as int? ?? 0,
                        Stock = reader["Stock"] as int? ?? 0
                    });
                }
            }
            return stockRecords;
        }
    }
}