using ClosedXML.Excel;
using ITHelpDesk_Updated.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ITHelpDesk_Updated.Controllers
{
    [NoCache]
    public class RiskPointsController : Controller
    {
        private readonly string connectionString = "Data Source=192.168.0.75;Initial Catalog=ITHD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";
        private readonly string connectionStringHRD = "Data Source=192.168.0.75;Initial Catalog=HRD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";


        // Display the form to create a new risk point and the list of existing risk points
        [HttpGet]
        public IActionResult CreateRiskPoint()
        {
            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch user info from session and HRD Database
            var userDetails = FetchUserDetailsFromHRD(USR_NAME);
            ViewData["USR_NAME"] = userDetails.UsrName;
            ViewData["USR_FULL_NAME"] = userDetails.UsrFullName;

            var model = new RiskPointViewModel
            {
                RiskPoints = FetchRiskPointsFromDatabase(),
                CurrentRiskPoint = new RiskPoint(),
                UserName = USR_NAME
            };

            return View("~/Views/ITHelpDesk/CreateRiskPoint.cshtml", model);
        }

        // Handle the form submission to create a new risk point
        [HttpPost]
        public IActionResult CreateRiskPoint(RiskPointViewModel model)
        {
            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            if (model?.CurrentRiskPoint == null)
            {
                ModelState.AddModelError("", "Invalid risk point data.");
                model.RiskPoints = FetchRiskPointsFromDatabase();
                return View("~/Views/ITHelpDesk/CreateRiskPoint.cshtml", model);
            }

            InsertRiskPointIntoDatabase(model.CurrentRiskPoint, USR_NAME);

            return RedirectToAction("CreateRiskPoint");
        }

        // Fetch user details from HRD Database
        private (string UsrName, string UsrFullName) FetchUserDetailsFromHRD(string usrName)
        {
            string usrFullName = string.Empty;

            using (SqlConnection connection = new SqlConnection(connectionStringHRD))
            {
                connection.Open();
                string query = "SELECT USR_FULL_NAME FROM HRD_SEC_USR_USERS WHERE USR_NAME = @USR_NAME";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@USR_NAME", usrName);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    usrFullName = reader["USR_FULL_NAME"] as string;
                }
            }

            return (usrName, usrFullName);
        }

        // Fetch all risk points (not just the current user's risk points)
        // Fetch risk points from the database for a specific user
        private List<RiskPoint> FetchRiskPointsFromDatabase(string usrName = null)
        {
            var riskPoints = new List<RiskPoint>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT rp.*, u.USR_FULL_NAME 
            FROM RiskPoints rp
            LEFT JOIN [HRD].[dbo].[HRD_SEC_USR_USERS] u 
            ON rp.USR_NAME = u.USR_NAME";

                if (!string.IsNullOrEmpty(usrName))
                {
                    query += " WHERE rp.USR_NAME = @USR_NAME";  // Add filter for a specific user
                }

                query += " ORDER BY rp.CreatedAt DESC";  // Order by creation date

                SqlCommand cmd = new SqlCommand(query, connection);

                if (!string.IsNullOrEmpty(usrName))
                {
                    cmd.Parameters.AddWithValue("@USR_NAME", usrName);  // Add parameter if filtering by user
                }

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    riskPoints.Add(new RiskPoint
                    {
                        RiskPointId = (int)reader["RiskPointId"],
                        Title = reader["Title"] as string ?? "No Issue Type",
                        Priority = reader["Priority"] as string ?? "Medium",
                        Status = reader["Status"] as string ?? "New",
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        ResolvedAt = reader["ResolvedAt"] as DateTime?,
                        Category = reader["Category"] as string,
                        USR_FULL_NAME = reader["USR_FULL_NAME"] as string,
                        USR_NAME = reader["USR_NAME"] as string
                    });
                }
            }

            return riskPoints;
        }

        // Insert a new risk point into the database
        private void InsertRiskPointIntoDatabase(RiskPoint riskPoint, string usrName)
        {
            var userDetails = FetchUserDetailsFromHRD(usrName);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    INSERT INTO RiskPoints (Title, Priority, Status, CreatedAt, ResolvedAt, USR_NAME, USR_FULL_NAME, Category) 
                    VALUES (@Title, @Priority, @Status, @CreatedAt, @ResolvedAt, @USR_NAME, @USR_FULL_NAME, @Category)";
                SqlCommand cmd = new SqlCommand(query, connection);

                cmd.Parameters.AddWithValue("@Title", riskPoint.Title ?? "No Issue Type");
                cmd.Parameters.AddWithValue("@Priority", riskPoint.Priority ?? "Medium");
                cmd.Parameters.AddWithValue("@Status", riskPoint.Status ?? "New");
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@ResolvedAt", riskPoint.ResolvedAt ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@USR_NAME", userDetails.UsrName);
                cmd.Parameters.AddWithValue("@USR_FULL_NAME", userDetails.UsrFullName ?? "Unknown User");
                cmd.Parameters.AddWithValue("@Category", riskPoint.Category ?? "Other");

                cmd.ExecuteNonQuery();
            }
        }

        // Update an existing risk point in the database
        private void UpdateRiskPointInDatabase(RiskPoint riskPoint)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    UPDATE RiskPoints 
                    SET Title = @Title, Priority = @Priority, Status = @Status, 
                        ResolvedAt = @ResolvedAt, Category = @Category 
                    WHERE RiskPointId = @RiskPointId";

                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Title", riskPoint.Title);
                cmd.Parameters.AddWithValue("@Priority", riskPoint.Priority);
                cmd.Parameters.AddWithValue("@Status", riskPoint.Status);
                cmd.Parameters.AddWithValue("@ResolvedAt", riskPoint.ResolvedAt ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@RiskPointId", riskPoint.RiskPointId);
                cmd.Parameters.AddWithValue("@Category", riskPoint.Category);

                cmd.ExecuteNonQuery();
            }
        }

        // Fetch the risk point by ID
        public IActionResult EditRiskPoint(int id)
        {
            var riskPoint = FetchRiskPointById(id);

            if (riskPoint == null)
            {
                return NotFound(); // Return a "Not Found" view if the risk point doesn't exist
            }

            // Fetch user info from session and HRD Database
            string USR_NAME = HttpContext.Session.GetString("USR_NAME");
            var userDetails = FetchUserDetailsFromHRD(USR_NAME);
            ViewData["USR_NAME"] = userDetails.UsrName;
            ViewData["USR_FULL_NAME"] = userDetails.UsrFullName;

            var model = new RiskPointViewModel
            {
                CurrentRiskPoint = riskPoint,
                RiskPoints = FetchRiskPointsFromDatabase(USR_NAME)
            };

            return View("~/Views/ITHelpDesk/EditRiskPoint.cshtml", model);
        }

        // Handle the update of a risk point
        [HttpPost]
        public IActionResult EditRiskPoint(RiskPointViewModel model)
        {
            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            if (model?.CurrentRiskPoint == null)
            {
                ModelState.AddModelError("", "Invalid risk point data.");
                model.RiskPoints = FetchRiskPointsFromDatabase(USR_NAME);
                return View("~/Views/ITHelpDesk/EditRiskPoint.cshtml", model);
            }

            UpdateRiskPointInDatabase(model.CurrentRiskPoint);

            return RedirectToAction("CreateRiskPoint");
        }

        // Helper method to fetch a single risk point by ID
        private RiskPoint FetchRiskPointById(int id)
        {
            RiskPoint riskPoint = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM RiskPoints WHERE RiskPointId = @RiskPointId";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@RiskPointId", id);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    riskPoint = new RiskPoint
                    {
                        RiskPointId = (int)reader["RiskPointId"],
                        Title = reader["Title"] as string,
                        Priority = reader["Priority"] as string,
                        Status = reader["Status"] as string,
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        ResolvedAt = reader["ResolvedAt"] as DateTime?,
                        Category = reader["Category"] as string
                    };
                }
            }

            return riskPoint;
        }

        // Fetch all resolved risk points (Achievements)
        public IActionResult AchievementsOverRiskPoints()
        {
            // Ensure the user is logged in
            if (HttpContext.Session.GetString("USR_NAME") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch resolved risk points from the database
            var resolvedRiskPoints = FetchResolvedRiskPointsFromDatabase();

            // Return the view with the resolved risk points and user information
            return View("~/Views/ITHelpDesk/AchievementsOverRiskPoints.cshtml", resolvedRiskPoints);
        }

        // Helper method to fetch resolved risk points from the database
        private List<RiskPoint> FetchResolvedRiskPointsFromDatabase()
        {

            var resolvedRiskPoints = new List<RiskPoint>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM RiskPoints WHERE Status = 'Resolved' ORDER BY ResolvedAt DESC";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    resolvedRiskPoints.Add(new RiskPoint
                    {
                        RiskPointId = (int)reader["RiskPointId"],
                        USR_NAME = reader["USR_NAME"] as string,
                        USR_FULL_NAME = reader["USR_FULL_NAME"] as string,
                        Title = reader["Title"] as string ?? "No Issue Type",
                        Priority = reader["Priority"] as string ?? "Medium",
                        Status = reader["Status"] as string ?? "New",
                        CreatedAt = reader["CreatedAt"] as DateTime? ?? DateTime.Now,
                        ResolvedAt = reader["ResolvedAt"] as DateTime?,
                        Category = reader["Category"] as string
                    });
                }
            }

            return resolvedRiskPoints;
        }

        [HttpPost]
        public IActionResult GenerateReport([FromBody] FilterModel filters)
        {
            // Get the filtered risk points based on the selected filters
            List<RiskPoint> filteredRiskPoints = GetFilteredRiskPoints(filters);

            // Generate the Excel file
            var excelFile = GenerateExcelFile(filteredRiskPoints, filters);

            // Return the Excel file as a downloadable file with the correct content type for Excel
            return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ResolvedRiskPointsReport.xlsx");
        }

        private List<RiskPoint> GetFilteredRiskPoints(FilterModel filters)
        {
            List<RiskPoint> filteredRiskPoints = new List<RiskPoint>();
            string queryStr = @"
                SELECT rp.*, u.USR_FULL_NAME 
                FROM RiskPoints rp
                LEFT JOIN [HRD].[dbo].[HRD_SEC_USR_USERS] u 
                ON rp.USR_NAME = u.USR_NAME
                WHERE rp.Status = 'Resolved'";

            // Apply filters dynamically
            if (!string.IsNullOrEmpty(filters.Username)) queryStr += " AND rp.USR_NAME = @USR_NAME";
            if (!string.IsNullOrEmpty(filters.Name)) queryStr += " AND u.USR_FULL_NAME = @Name";
            if (!string.IsNullOrEmpty(filters.Title)) queryStr += " AND rp.Title LIKE @Title";
            if (!string.IsNullOrEmpty(filters.Category)) queryStr += " AND rp.Category = @Category";
            if (!string.IsNullOrEmpty(filters.Priority)) queryStr += " AND rp.Priority = @Priority";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(queryStr, connection);

                // Add parameters dynamically based on filters
                if (!string.IsNullOrEmpty(filters.Username)) cmd.Parameters.AddWithValue("@USR_NAME", filters.Username);
                if (!string.IsNullOrEmpty(filters.Name)) cmd.Parameters.AddWithValue("@Name", filters.Name);
                if (!string.IsNullOrEmpty(filters.Title)) cmd.Parameters.AddWithValue("@Title", "%" + filters.Title + "%");
                if (!string.IsNullOrEmpty(filters.Category)) cmd.Parameters.AddWithValue("@Category", filters.Category);
                if (!string.IsNullOrEmpty(filters.Priority)) cmd.Parameters.AddWithValue("@Priority", filters.Priority);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    filteredRiskPoints.Add(new RiskPoint
                    {
                        RiskPointId = (int)reader["RiskPointId"],
                        Title = reader["Title"] as string ?? "No Issue Type",
                        Priority = reader["Priority"] as string ?? "Medium",
                        Status = reader["Status"] as string ?? "Resolved",
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        ResolvedAt = reader["ResolvedAt"] as DateTime?,
                        Category = reader["Category"] as string,
                        USR_FULL_NAME = reader["USR_FULL_NAME"] as string,
                        USR_NAME = reader["USR_NAME"] as string
                    });
                }
            }

            return filteredRiskPoints;
        }

        private byte[] GenerateExcelFile(List<RiskPoint> filteredRiskPoints, FilterModel filters)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Resolved Risk Points");

                // Table headers
                worksheet.Cell(1, 1).Value = "Employee Code";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "Title";
                worksheet.Cell(1, 4).Value = "Category";
                worksheet.Cell(1, 5).Value = "Priority";
                worksheet.Cell(1, 6).Value = "Status";
                worksheet.Cell(1, 7).Value = "Created At";
                worksheet.Cell(1, 8).Value = "Resolved At";

                worksheet.Row(1).Style.Font.Bold = true;
                worksheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Adding Data Rows
                int row = 2;
                foreach (var riskPoint in filteredRiskPoints)
                {
                    worksheet.Cell(row, 1).Value = riskPoint.USR_NAME;
                    worksheet.Cell(row, 2).Value = riskPoint.USR_FULL_NAME;
                    worksheet.Cell(row, 3).Value = riskPoint.Title;
                    worksheet.Cell(row, 4).Value = riskPoint.Category;
                    worksheet.Cell(row, 5).Value = riskPoint.Priority;
                    worksheet.Cell(row, 6).Value = riskPoint.Status;
                    worksheet.Cell(row, 7).Value = riskPoint.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, 8).Value = riskPoint.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not Resolved";
                    row++;
                }

                // Adding borders to the table
                var range = worksheet.Range("A1:H" + (row - 1));
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                using (var memoryStream = new MemoryStream())
                {
                    workbook.SaveAs(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        [HttpPost]
        public IActionResult GenerateAllRiskPointsReport()
        {
            // Get all risk points without filters
            List<RiskPoint> allRiskPoints = GetAllRiskPoints();

            // Generate the Excel file for all risk points
            var excelFile = GenerateExcelFile(allRiskPoints);

            // Return the Excel file as a downloadable file
            return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "RiskPointsReport.xlsx");
        }
        private List<RiskPoint> GetAllRiskPoints()
        {
            List<RiskPoint> allRiskPoints = new List<RiskPoint>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT rp.*, u.USR_FULL_NAME 
                    FROM RiskPoints rp
                    LEFT JOIN [HRD].[dbo].[HRD_SEC_USR_USERS] u 
                    ON rp.USR_NAME = u.USR_NAME
                    ORDER BY rp.CreatedAt DESC";

                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    allRiskPoints.Add(new RiskPoint
                    {
                        RiskPointId = (int)reader["RiskPointId"],
                        Title = reader["Title"] as string ?? "No Issue Type",
                        Priority = reader["Priority"] as string ?? "Medium",
                        Status = reader["Status"] as string ?? "New",
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        ResolvedAt = reader["ResolvedAt"] as DateTime?,
                        Category = reader["Category"] as string,
                        USR_FULL_NAME = reader["USR_FULL_NAME"] as string,
                        USR_NAME = reader["USR_NAME"] as string
                    });
                }
            }

            return allRiskPoints;
        }
        private byte[] GenerateExcelFile(List<RiskPoint> allRiskPoints)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("All Risk Points");

                // Table headers
                worksheet.Cell(1, 1).Value = "Employee Code";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "Title";
                worksheet.Cell(1, 4).Value = "Category";
                worksheet.Cell(1, 5).Value = "Priority";
                worksheet.Cell(1, 6).Value = "Status";
                worksheet.Cell(1, 7).Value = "Created At";
                worksheet.Cell(1, 8).Value = "Resolved At";

                worksheet.Row(1).Style.Font.Bold = true;
                worksheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Adding Data Rows
                int row = 2;
                foreach (var riskPoint in allRiskPoints)
                {
                    worksheet.Cell(row, 1).Value = riskPoint.USR_NAME;
                    worksheet.Cell(row, 2).Value = riskPoint.USR_FULL_NAME;
                    worksheet.Cell(row, 3).Value = riskPoint.Title;
                    worksheet.Cell(row, 4).Value = riskPoint.Category;
                    worksheet.Cell(row, 5).Value = riskPoint.Priority;
                    worksheet.Cell(row, 6).Value = riskPoint.Status;
                    worksheet.Cell(row, 7).Value = riskPoint.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, 8).Value = riskPoint.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Not Resolved";
                    row++;
                }

                // Adding borders to the table
                var range = worksheet.Range("A1:H" + (row - 1));
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
