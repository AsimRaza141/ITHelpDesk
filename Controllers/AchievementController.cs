using ClosedXML.Excel;
using ITHelpDesk_Updated.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ITHelpDesk_Updated.Controllers
{
    [NoCache]
    public class AchievementController : Controller
    {
        private readonly string connectionString = "Data Source=192.168.0.75;Initial Catalog=ITHD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";
        private readonly string connectionStringHRD = "Data Source=192.168.0.75;Initial Catalog=HRD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

        // Action to display the Create Achievement form and list of achievements
        [HttpGet]
        public IActionResult CreateAchievement()
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

            List<Achievement> achievements = new List<Achievement>();

            // Fetch the list of achievements from the ITHD database, including USR_NAME and USR_FULL_NAME
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT a.*, u.USR_NAME, u.USR_FULL_NAME
                    FROM Achievements a
                    LEFT JOIN [HRD].[dbo].[HRD_SEC_USR_USERS] u ON a.USR_NAME = u.USR_NAME
                    ORDER BY a.DateAchieved DESC";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    achievements.Add(new Achievement
                    {
                        AchievementId = (int)reader["AchievementId"],
                        Title = reader["Title"] as string ?? "No Title",
                        Description = reader["Description"] as string ?? "No Description",
                        DateAchieved = reader["DateAchieved"] as DateTime?,
                        USR_NAME = reader["USR_NAME"] as string,
                        USR_FULL_NAME = reader["USR_FULL_NAME"] as string
                    });
                }
            }

            var model = new AchievementViewModel
            {
                Achievements = achievements ?? new List<Achievement>(),
                CurrentAchievement = new Achievement()
            };

            return View("~/Views/ITHelpDesk/CreateAchievement.cshtml", model);
        }

        // Action to handle form submission for creating a new achievement
        [HttpPost]
        public IActionResult CreateAchievement(AchievementViewModel model)
        {
            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            string USR_FULL_NAME = HttpContext.Session.GetString("USR_FULL_NAME");

            if (model?.CurrentAchievement == null)
            {
                ModelState.AddModelError("", "Invalid achievement data.");
                return View("~/Views/ITHelpDesk/CreateAchievement.cshtml", model);
            }

            // Insert the new achievement into the ITHD database
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Achievements (Title, Description, DateAchieved, USR_NAME, USR_FULL_NAME) " +
                               "VALUES (@Title, @Description, @DateAchieved, @USR_NAME, @USR_FULL_NAME)";
                SqlCommand cmd = new SqlCommand(query, connection);

                cmd.Parameters.AddWithValue("@Title", model.CurrentAchievement.Title ?? "Untitled");
                cmd.Parameters.AddWithValue("@Description", model.CurrentAchievement.Description ?? "No description provided");
                cmd.Parameters.AddWithValue("@DateAchieved", model.CurrentAchievement.DateAchieved.HasValue ? model.CurrentAchievement.DateAchieved.Value : DateTime.Now);
                cmd.Parameters.AddWithValue("@USR_NAME", USR_NAME);
                cmd.Parameters.AddWithValue("@USR_FULL_NAME", USR_FULL_NAME ?? "Unknown");

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("CreateAchievement");
        }

        // Helper method to fetch user details (USR_NAME and USR_FULL_NAME) from HRD database
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

        [HttpPost]
        public IActionResult GenerateAchievementReport()
        {
            // Fetch all achievements
            List<Achievement> allAchievements = GetAllAchievements();

            // Generate the Excel file for achievements
            var excelFile = GenerateExcelFile(allAchievements);

            // Return the Excel file as a downloadable file
            return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AchievementsReport.xlsx");
        }

        private List<Achievement> GetAllAchievements()
        {
            List<Achievement> allAchievements = new List<Achievement>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                SELECT a.*, u.USR_FULL_NAME 
                FROM Achievements a
                LEFT JOIN [HRD].[dbo].[HRD_SEC_USR_USERS] u 
                ON a.USR_NAME = u.USR_NAME
                ORDER BY a.DateAchieved DESC";

                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    allAchievements.Add(new Achievement
                    {
                        AchievementId = (int)reader["AchievementId"],
                        Title = reader["Title"] as string ?? "No Title",
                        Description = reader["Description"] as string ?? "No Description",
                        DateAchieved = reader["DateAchieved"] as DateTime?,
                        USR_NAME = reader["USR_NAME"] as string,
                        USR_FULL_NAME = reader["USR_FULL_NAME"] as string
                    });
                }
            }

            return allAchievements;
        }

        private byte[] GenerateExcelFile(List<Achievement> allAchievements)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Achievements");

                // Table headers
                worksheet.Cell(1, 1).Value = "Employee Code";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "Title";
                worksheet.Cell(1, 4).Value = "Description";
                worksheet.Cell(1, 5).Value = "Date Achieved";

                worksheet.Row(1).Style.Font.Bold = true;
                worksheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Adding Data Rows
                int row = 2;
                foreach (var achievement in allAchievements)
                {
                    worksheet.Cell(row, 1).Value = achievement.USR_NAME;
                    worksheet.Cell(row, 2).Value = achievement.USR_FULL_NAME;
                    worksheet.Cell(row, 3).Value = achievement.Title;
                    worksheet.Cell(row, 4).Value = achievement.Description;
                    worksheet.Cell(row, 5).Value = achievement.DateAchieved?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                    row++;
                }

                // Adding borders to the table
                var range = worksheet.Range("A1:E" + (row - 1));
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
