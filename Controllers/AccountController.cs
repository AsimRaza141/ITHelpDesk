using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ITHelpDesk_Updated.Controllers
{
    [NoCache]
    public class AccountController : Controller
    {
        private readonly string connectionString = "Data Source=192.168.0.75;Initial Catalog=HRD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

        // Login GET Action
        public IActionResult Login()
        {
            // If user is already logged in, redirect them to Selection page
            if (HttpContext.Session.GetString("USR_NAME") != null)
            {
                return RedirectToAction("Selection");
            }

            return View("~/Views/ITHelpDesk/Login.cshtml");
        }

        // Login POST Action
        [HttpPost]
        public IActionResult Login(string USR_NAME, string USR_PASSWORD)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT USR_FULL_NAME FROM HRD_SEC_USR_USERS WHERE USR_NAME = @USR_NAME AND USR_PASSWORD = @USR_PASSWORD";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@USR_NAME", USR_NAME);
                cmd.Parameters.AddWithValue("@USR_PASSWORD", USR_PASSWORD);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    string fullName = reader["USR_FULL_NAME"].ToString();

                    // Store the full name and username in the session
                    HttpContext.Session.SetString("USR_NAME", USR_NAME);
                    HttpContext.Session.SetString("USR_FULL_NAME", fullName);

                    TempData["WelcomeMessage"] = $"Welcome, {fullName}!";

                    // Pass the logged-in user info to the View
                    ViewData["USR_NAME"] = USR_NAME; // Pass session value to the view

                    // Redirect to the Selection page after login
                    return RedirectToAction("Selection");
                }
                else
                {
                    ViewBag.ErrorMessage = "Invalid Credentials.";
                    return View("~/Views/ITHelpDesk/Login.cshtml");
                }
            }
        }

        // Selection Action
        public IActionResult Selection()
        {
            // If the user is not logged in, redirect to the login page
            if (HttpContext.Session.GetString("USR_NAME") == null)
            {
                return RedirectToAction("Login");
            }

            // Pass logged-in user info to the view
            ViewData["USR_NAME"] = HttpContext.Session.GetString("USR_NAME");

            return View("~/Views/ITHelpDesk/Selection.cshtml");
        }

        // Logout Action
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
