using System.Text;
using ClosedXML.Excel;
using ITHelpDesk_Updated.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ITHelpDesk_Updated.Controllers
{
    [NoCache]
    public class QueryController : Controller
    {
        private readonly string connectionString = "Data Source=192.168.0.75;Initial Catalog=ITHD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

        // Action to display the Create Query form and list of queries
        [HttpGet]
        public IActionResult CreateQuery()
        {
            ViewData["Title"] = "Create and View Queries";

            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            var ticketNumber = GetNextTicketNumber();
            ViewData["TicketNumber"] = ticketNumber;

            List<Query> queries = new List<Query>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                                SELECT q.*, u.USR_FULL_NAME 
                                FROM Queries q
                                LEFT JOIN [HRD].[dbo].[HRD_SEC_USR_USERS] u 
                                ON q.USR_NAME = u.USR_NAME
                                WHERE q.USR_NAME = @USR_NAME ORDER BY q.CreatedAt DESC";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@USR_NAME", USR_NAME);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    queries.Add(new Query
                    {
                        QueryId = (int)reader["QueryId"],
                        TicketNumber = reader["TicketNumber"] as string,
                        USR_NAME = reader["USR_NAME"] != DBNull.Value ? (string)reader["USR_NAME"] : null,
                        USR_FULL_NAME = reader["USR_FULL_NAME"] != DBNull.Value ? (string)reader["USR_FULL_NAME"] : null,
                        Title = reader["Title"] != DBNull.Value ? (string)reader["Title"] : null,
                        Description = reader["Description"] != DBNull.Value ? (string)reader["Description"] : null,
                        Priority = reader["Priority"] != DBNull.Value ? (string)reader["Priority"] : null,
                        Category = reader["Category"] != DBNull.Value ? (string)reader["Category"] : null,
                        Status = reader["Status"] != DBNull.Value ? (string)reader["Status"] : null,
                        AssignedTo = reader["AssignedTo"] != DBNull.Value ? (string)reader["AssignedTo"] : null,
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        ResolvedAt = reader["ResolvedAt"] != DBNull.Value ? (DateTime?)reader["ResolvedAt"] : null
                    });
                }
            }

            var model = new QueryViewModel
            {
                Queries = queries,
                CurrentQuery = new Query(),
                UserName = USR_NAME,
                AssignedUsers = new List<string>()
            };

            return View("~/Views/ITHelpDesk/CreateQuery.cshtml", model);
        }

        // Helper method to get the next ticket number by incrementing the last ticket number
        private string GetNextTicketNumber()
        {
            string newTicketNumber = "IT-101";  // Default fallback ticket number

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Query to get the latest ticket number
                string queryStr = "SELECT TOP 1 TicketNumber FROM Queries ORDER BY TicketNumber DESC";

                SqlCommand cmd = new SqlCommand(queryStr, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string lastTicketNumber = reader["TicketNumber"] as string;

                    if (!string.IsNullOrEmpty(lastTicketNumber))
                    {
                        // Extract the numeric part of the last ticket number and increment it
                        int lastTicketNum = int.Parse(lastTicketNumber.Substring(3)); // Remove "IT-" prefix
                        newTicketNumber = "IT-" + (lastTicketNum + 1).ToString();
                    }
                }
            }

            return newTicketNumber;
        }

        // This action will fetch the USR_FULL_NAME based on Category
        [HttpPost]
        public JsonResult GetAssignedUsersByCategory(string category)
        {
            List<string> assignedUsers = new List<string>();

            string query = "";

            switch (category)
            {
                case "Hardware":
                    query = "SELECT USR_FULL_NAME FROM HRD_SEC_USR_USERS WHERE USR_NAME IN (4357, 1078, 3961)";
                    break;
                case "ERPSupport":
                    query = "SELECT USR_FULL_NAME FROM HRD_SEC_USR_USERS WHERE USR_NAME IN (4762, 3936, 4654)";
                    break;
                case "Software":
                    query = "SELECT USR_FULL_NAME FROM HRD_SEC_USR_USERS WHERE USR_NAME IN (4654, 4999)";
                    break;
                case "Purchase":
                    query = "SELECT USR_FULL_NAME FROM HRD_SEC_USR_USERS WHERE USR_NAME IN (4357)";
                    break;
                case "Other":
                    query = "SELECT USR_FULL_NAME FROM HRD_SEC_USR_USERS WHERE USR_NAME IN (4357, 1078, 3961, 4762, 3936, 4654, 4999, 1042)";
                    break;
                default:
                    break;
            }

            // Debug: Log category to ensure it's being passed correctly
            Console.WriteLine("Category: " + category);

            // Connect to HRD database for fetching user names
            string connectionStringHRD = "Data Source=192.168.0.75;Initial Catalog=HRD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

            using (SqlConnection connection = new SqlConnection(connectionStringHRD))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    assignedUsers.Add(reader["USR_FULL_NAME"] as string);
                }
            }

            return Json(assignedUsers);
        }

        [HttpPost]
        public IActionResult CreateQuery(QueryViewModel model)
        {
            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            Query query = model.CurrentQuery;

            // Get the next ticket number by incrementing
            query.TicketNumber = GetNextTicketNumber();

            DateTime? resolvedAt = query.ResolvedAt;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string insertQuery = "INSERT INTO Queries (TicketNumber, USR_NAME, Title, Description, Priority, Category, Status, CreatedAt, ResolvedAt, AssignedTo) " +
                                     "VALUES (@TicketNumber, @USR_NAME, @Title, @Description, @Priority, @Category, @Status, @CreatedAt, @ResolvedAt, @AssignedTo)";

                SqlCommand cmd = new SqlCommand(insertQuery, connection);

                cmd.Parameters.AddWithValue("@TicketNumber", query.TicketNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@USR_NAME", USR_NAME ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Title", query.Title ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", query.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Priority", query.Priority ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Category", query.Category ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", query.Status ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@ResolvedAt", resolvedAt ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@AssignedTo", query.AssignedTo ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("CreateQuery");
        }

        [HttpGet]
        public IActionResult QueriesList()
        {
            ViewData["Title"] = "Queries List";

            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            var allowedUsers = new List<string> { "4357", "1078", "4922", "3961", "4762", "3936", "4654", "4999", "1042", "1830", "1831", "2560" };
            if (!allowedUsers.Contains(USR_NAME))
            {
                return Unauthorized();
            }

            List<Query> queries = new List<Query>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT q.*, u.USR_FULL_NAME 
                    FROM Queries q
                    LEFT JOIN [HRD].[dbo].[HRD_SEC_USR_USERS] u 
                    ON q.USR_NAME = u.USR_NAME ORDER BY q.CreatedAt DESC";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    queries.Add(new Query
                    {
                        QueryId = (int)reader["QueryId"],
                        TicketNumber = reader["TicketNumber"] as string,
                        USR_NAME = reader["USR_NAME"] != DBNull.Value ? (string)reader["USR_NAME"] : null,
                        USR_FULL_NAME = reader["USR_FULL_NAME"] != DBNull.Value ? (string)reader["USR_FULL_NAME"] : null,
                        Title = reader["Title"] != DBNull.Value ? (string)reader["Title"] : null,
                        Description = reader["Description"] != DBNull.Value ? (string)reader["Description"] : null,
                        Priority = reader["Priority"] != DBNull.Value ? (string)reader["Priority"] : null,
                        Category = reader["Category"] != DBNull.Value ? (string)reader["Category"] : null,
                        Status = reader["Status"] != DBNull.Value ? (string)reader["Status"] : null,
                        AssignedTo = reader["AssignedTo"] != DBNull.Value ? (string)reader["AssignedTo"] : null,
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        ResolvedAt = reader["ResolvedAt"] != DBNull.Value ? (DateTime?)reader["ResolvedAt"] : null
                    });
                }
            }

            ViewData["CurrentUser"] = USR_NAME;
            return View("~/Views/ITHelpDesk/QueriesList.cshtml", queries);
        }

        // Edit Query - GET
        [HttpGet]
        public IActionResult EditQuery(int id)
        {
            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            Query query = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string queryStr = @"
                    SELECT q.*, u.USR_FULL_NAME 
                    FROM Queries q
                    LEFT JOIN [HRD].[dbo].[HRD_SEC_USR_USERS] u 
                    ON q.USR_NAME = u.USR_NAME
                    WHERE q.QueryId = @QueryId";
                SqlCommand cmd = new SqlCommand(queryStr, connection);
                cmd.Parameters.AddWithValue("@QueryId", id);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    query = new Query
                    {
                        QueryId = (int)reader["QueryId"],
                        TicketNumber = reader["TicketNumber"] as string,
                        USR_NAME = reader["USR_NAME"] != DBNull.Value ? (string)reader["USR_NAME"] : null,
                        USR_FULL_NAME = reader["USR_FULL_NAME"] != DBNull.Value ? (string)reader["USR_FULL_NAME"] : null,
                        Title = reader["Title"] != DBNull.Value ? (string)reader["Title"] : null,
                        Description = reader["Description"] != DBNull.Value ? (string)reader["Description"] : null,
                        Priority = reader["Priority"] != DBNull.Value ? (string)reader["Priority"] : null,
                        Category = reader["Category"] != DBNull.Value ? (string)reader["Category"] : null,
                        Status = reader["Status"] != DBNull.Value ? (string)reader["Status"] : null,
                        AssignedTo = reader["AssignedTo"] != DBNull.Value ? (string)reader["AssignedTo"] : null,
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        ResolvedAt = reader["ResolvedAt"] != DBNull.Value ? (DateTime?)reader["ResolvedAt"] : null
                    };
                }
            }

            if (query == null)
            {
                return NotFound();
            }

            var model = new QueryViewModel
            {
                CurrentQuery = query,
                Queries = new List<Query>(),
                UserName = USR_NAME,
                AssignedUsers = new List<string>()
            };

            return View("~/Views/ITHelpDesk/EditQuery.cshtml", model);
        }

        // Edit Query - POST
        [HttpPost]
        public IActionResult EditQuery(QueryViewModel model)
        {
            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            Query query = model.CurrentQuery;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string queryStr = @"
                    UPDATE Queries 
                    SET Title = @Title, Description = @Description, Priority = @Priority, 
                        Category = @Category, AssignedTo = @AssignedTo, Status = @Status,
                        ResolvedAt = CASE 
                            WHEN @Status = 'Resolved' AND ResolvedAt IS NULL THEN @CurrentDateTime
                            ELSE ResolvedAt 
                        END
                    WHERE QueryId = @QueryId";
                SqlCommand cmd = new SqlCommand(queryStr, connection);

                cmd.Parameters.AddWithValue("@QueryId", query.QueryId);
                cmd.Parameters.AddWithValue("@Title", query.Title ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", query.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Priority", query.Priority ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Category", query.Category ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@AssignedTo", query.AssignedTo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", query.Status ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CurrentDateTime", DateTime.Now);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    return NotFound();
                }
            }

            // Check if the user has access to QueriesList
            var allowedUsers = new List<string> { "4357", "1078", "4922", "3961", "4762", "3936", "4654", "4999", "1042", "1830", "1831", "2560" };
            bool isAllowedUser = allowedUsers.Contains(USR_NAME);
            return isAllowedUser ? RedirectToAction("QueriesList") : RedirectToAction("CreateQuery");
        }

        // Action for generating the filtered report
        [HttpPost]
        public IActionResult GenerateReport([FromBody] FilterModel filters)
        {
            // Get filtered queries based on the selected filters
            List<Query> filteredQueries = GetFilteredQueries(filters);

            // Generate the Excel file
            var excelFile = GenerateExcelFile(filteredQueries, filters);

            // Return the Excel file as a downloadable file with the correct content type for Excel
            return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "QueriesReport.xlsx");
        }

        private List<Query> GetFilteredQueries(FilterModel filters)
        {
            List<Query> filteredQueries = new List<Query>();
            string queryStr = @"
                SELECT q.*, u.USR_FULL_NAME 
                FROM Queries q
                LEFT JOIN [HRD].[dbo].[HRD_SEC_USR_USERS] u 
                ON q.USR_NAME = u.USR_NAME
                WHERE 1 = 1";  // Base condition that always returns true

            // Apply the filters dynamically
            if (!string.IsNullOrEmpty(filters.Username)) queryStr += " AND q.USR_NAME = @USR_NAME";
            if (!string.IsNullOrEmpty(filters.Name)) queryStr += " AND u.USR_FULL_NAME = @Name";
            if (!string.IsNullOrEmpty(filters.TicketNumber)) queryStr += " AND q.TicketNumber = @TicketNumber";
            if (!string.IsNullOrEmpty(filters.Title)) queryStr += " AND q.Title LIKE @Title";
            if (!string.IsNullOrEmpty(filters.Priority)) queryStr += " AND q.Priority = @Priority";
            if (!string.IsNullOrEmpty(filters.Category)) queryStr += " AND q.Category = @Category";
            if (!string.IsNullOrEmpty(filters.AssignedTo)) queryStr += " AND q.AssignedTo = @AssignedTo";
            if (!string.IsNullOrEmpty(filters.Status)) queryStr += " AND q.Status = @Status";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(queryStr, connection);

                // Add parameters dynamically based on filters
                if (!string.IsNullOrEmpty(filters.Username)) cmd.Parameters.AddWithValue("@USR_NAME", filters.Username);
                if (!string.IsNullOrEmpty(filters.Name)) cmd.Parameters.AddWithValue("@Name", filters.Name);
                if (!string.IsNullOrEmpty(filters.TicketNumber)) cmd.Parameters.AddWithValue("@TicketNumber", filters.TicketNumber);
                if (!string.IsNullOrEmpty(filters.Title)) cmd.Parameters.AddWithValue("@Title", "%" + filters.Title + "%");
                if (!string.IsNullOrEmpty(filters.Priority)) cmd.Parameters.AddWithValue("@Priority", filters.Priority);
                if (!string.IsNullOrEmpty(filters.Category)) cmd.Parameters.AddWithValue("@Category", filters.Category);
                if (!string.IsNullOrEmpty(filters.AssignedTo)) cmd.Parameters.AddWithValue("@AssignedTo", filters.AssignedTo);
                if (!string.IsNullOrEmpty(filters.Status)) cmd.Parameters.AddWithValue("@Status", filters.Status);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    filteredQueries.Add(new Query
                    {
                        QueryId = (int)reader["QueryId"],
                        TicketNumber = reader["TicketNumber"] as string,
                        USR_NAME = reader["USR_NAME"] != DBNull.Value ? (string)reader["USR_NAME"] : null,
                        USR_FULL_NAME = reader["USR_FULL_NAME"] != DBNull.Value ? (string)reader["USR_FULL_NAME"] : null,
                        Title = reader["Title"] != DBNull.Value ? (string)reader["Title"] : null,
                        Description = reader["Description"] != DBNull.Value ? (string)reader["Description"] : null,
                        Priority = reader["Priority"] != DBNull.Value ? (string)reader["Priority"] : null,
                        Category = reader["Category"] != DBNull.Value ? (string)reader["Category"] : null,
                        Status = reader["Status"] != DBNull.Value ? (string)reader["Status"] : null,
                        AssignedTo = reader["AssignedTo"] != DBNull.Value ? (string)reader["AssignedTo"] : null,
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        ResolvedAt = reader["ResolvedAt"] != DBNull.Value ? (DateTime?)reader["ResolvedAt"] : null
                    });
                }
            }

            return filteredQueries;
        }

        private byte[] GenerateExcelFile(List<Query> filteredQueries, FilterModel filters)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Queries");

                // Table headers
                worksheet.Cell(1, 1).Value = "Ticket Number";
                worksheet.Cell(1, 2).Value = "Employee Code";
                worksheet.Cell(1, 3).Value = "Name";
                worksheet.Cell(1, 4).Value = "Title";
                worksheet.Cell(1, 5).Value = "Description";
                worksheet.Cell(1, 6).Value = "Priority";
                worksheet.Cell(1, 7).Value = "Category";
                worksheet.Cell(1, 8).Value = "Status";
                worksheet.Cell(1, 9).Value = "Assigned To";
                worksheet.Cell(1, 10).Value = "Created At";
                worksheet.Cell(1, 11).Value = "Resolved At";

                worksheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Row(1).Style.Font.Bold = true;

                // Adding Data Rows
                int row = 2;
                foreach (var query in filteredQueries)
                {
                    worksheet.Cell(row, 1).Value = query.TicketNumber;
                    worksheet.Cell(row, 2).Value = query.USR_NAME;
                    worksheet.Cell(row, 3).Value = query.USR_FULL_NAME;
                    worksheet.Cell(row, 4).Value = query.Title;
                    worksheet.Cell(row, 5).Value = query.Description;
                    worksheet.Cell(row, 6).Value = query.Priority;
                    worksheet.Cell(row, 7).Value = query.Category;
                    worksheet.Cell(row, 8).Value = query.Status;
                    worksheet.Cell(row, 9).Value = query.AssignedTo;
                    worksheet.Cell(row, 10).Value = query.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, 11).Value = query.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not Resolved";
                    row++;
                }

                // Adding borders to the table
                var range = worksheet.Range("A1:L" + (row - 1));
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                using (var memoryStream = new MemoryStream())
                {
                    workbook.SaveAs(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
    }
}



