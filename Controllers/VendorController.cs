using ITHelpDesk_Updated.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ITHelpDesk_Updated.Controllers
{
    [NoCache]
    public class VendorController : Controller
    {
        private readonly string connectionString = "Data Source=192.168.0.75;Initial Catalog=ITHD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

        [HttpGet]
        public IActionResult CreateVendor()
        {
            if (HttpContext.Session.GetString("USR_NAME") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Clear model state to prevent stale data
            ModelState.Clear();

            var model = new VendorViewModel
            {
                CurrentVendor = new Vendor(),
                Vendors = GetAllVendors(),
                IsEditMode = false // Explicitly set to create mode
            };

            string nextVendorCode = GetNextVendorCode();
            model.CurrentVendor.VendorCode = nextVendorCode;
            ViewData["NextVendorCode"] = nextVendorCode; // Explicitly set

            return View("~/Views/ITHelpDesk/CreateVendor.cshtml", model);
        }

        [HttpPost]
        public IActionResult CreateVendor(VendorViewModel model)
        {
            if (HttpContext.Session.GetString("USR_NAME") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ModelState.Remove("Vendors");
            ModelState.Remove("IsEditMode");

            if (ModelState.IsValid)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string insertQuery = "INSERT INTO Vendors (VendorCode, VendorName, VendorNTN, VendorCNIC, VendorContactNo, VendorContactPerson) " +
                                        "VALUES (@VendorCode, @VendorName, @VendorNTN, @VendorCNIC, @VendorContactNo, @VendorContactPerson)";
                    SqlCommand cmd = new SqlCommand(insertQuery, connection);

                    cmd.Parameters.AddWithValue("@VendorCode", model.CurrentVendor.VendorCode ?? "");
                    cmd.Parameters.AddWithValue("@VendorName", model.CurrentVendor.VendorName ?? "Unknown");
                    cmd.Parameters.AddWithValue("@VendorNTN", model.CurrentVendor.VendorNTN ?? "Unknown");
                    cmd.Parameters.AddWithValue("@VendorCNIC", model.CurrentVendor.VendorCNIC ?? "Unknown");
                    cmd.Parameters.AddWithValue("@VendorContactNo", model.CurrentVendor.VendorContactNo ?? "Unknown");
                    cmd.Parameters.AddWithValue("@VendorContactPerson", model.CurrentVendor.VendorContactPerson ?? "Unknown");

                    cmd.ExecuteNonQuery();
                }

                return RedirectToAction("CreateVendor");
            }

            model.Vendors = GetAllVendors();
            model.IsEditMode = false; // Ensure create mode on validation failure
            ViewData["NextVendorCode"] = model.CurrentVendor.VendorCode; // Maintain NextVendorCode
            return View("~/Views/ITHelpDesk/CreateVendor.cshtml", model);
        }

        [HttpGet]
        public IActionResult EditVendor(string vendorCode)
        {
            if (HttpContext.Session.GetString("USR_NAME") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            Vendor vendor = GetVendorByCode(vendorCode);
            if (vendor == null)
            {
                return NotFound();
            }

            var model = new VendorViewModel
            {
                CurrentVendor = vendor,
                Vendors = GetAllVendors(),
                IsEditMode = true // Explicitly set to edit mode
            };

            ViewData["NextVendorCode"] = GetNextVendorCode(); // Set for consistency

            return View("~/Views/ITHelpDesk/CreateVendor.cshtml", model);
        }

        [HttpPost]
        public IActionResult EditVendor(VendorViewModel model)
        {
            if (HttpContext.Session.GetString("USR_NAME") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ModelState.Remove("Vendors");
            ModelState.Remove("IsEditMode");

            if (ModelState.IsValid)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string updateQuery = "UPDATE Vendors SET VendorName = @VendorName, VendorNTN = @VendorNTN, VendorCNIC = @VendorCNIC, " +
                                        "VendorContactNo = @VendorContactNo, VendorContactPerson = @VendorContactPerson WHERE VendorCode = @VendorCode";
                    SqlCommand cmd = new SqlCommand(updateQuery, connection);

                    cmd.Parameters.AddWithValue("@VendorCode", model.CurrentVendor.VendorCode ?? "");
                    cmd.Parameters.AddWithValue("@VendorName", model.CurrentVendor.VendorName ?? "Unknown");
                    cmd.Parameters.AddWithValue("@VendorNTN", model.CurrentVendor.VendorNTN ?? "Unknown");
                    cmd.Parameters.AddWithValue("@VendorCNIC", model.CurrentVendor.VendorCNIC ?? "Unknown");
                    cmd.Parameters.AddWithValue("@VendorContactNo", model.CurrentVendor.VendorContactNo ?? "Unknown");
                    cmd.Parameters.AddWithValue("@VendorContactPerson", model.CurrentVendor.VendorContactPerson ?? "Unknown");

                    cmd.ExecuteNonQuery();
                }

                return RedirectToAction("CreateVendor");
            }

            model.Vendors = GetAllVendors();
            model.IsEditMode = true; // Maintain edit mode on validation failure
            ViewData["NextVendorCode"] = GetNextVendorCode(); // Set for consistency
            return View("~/Views/ITHelpDesk/CreateVendor.cshtml", model);
        }

        private List<Vendor> GetAllVendors()
        {
            List<Vendor> vendors = new List<Vendor>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string queryStr = "SELECT VendorCode, VendorName, VendorNTN, VendorCNIC, VendorContactNo, VendorContactPerson FROM Vendors";
                SqlCommand cmd = new SqlCommand(queryStr, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    vendors.Add(new Vendor
                    {
                        VendorCode = reader["VendorCode"]?.ToString(),
                        VendorName = reader["VendorName"]?.ToString(),
                        VendorNTN = reader["VendorNTN"]?.ToString(),
                        VendorCNIC = reader["VendorCNIC"]?.ToString(),
                        VendorContactNo = reader["VendorContactNo"]?.ToString(),
                        VendorContactPerson = reader["VendorContactPerson"]?.ToString(),
                    });
                }
            }
            return vendors;
        }

        private Vendor GetVendorByCode(string vendorCode)
        {
            Vendor vendor = null;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT VendorCode, VendorName, VendorNTN, VendorCNIC, VendorContactNo, VendorContactPerson FROM Vendors WHERE VendorCode = @VendorCode";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@VendorCode", vendorCode ?? "");

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    vendor = new Vendor
                    {
                        VendorCode = reader["VendorCode"]?.ToString(),
                        VendorName = reader["VendorName"]?.ToString(),
                        VendorNTN = reader["VendorNTN"]?.ToString(),
                        VendorCNIC = reader["VendorCNIC"]?.ToString(),
                        VendorContactNo = reader["VendorContactNo"]?.ToString(),
                        VendorContactPerson = reader["VendorContactPerson"]?.ToString(),
                    };
                }
            }
            return vendor;
        }

        private string GetNextVendorCode()
        {
            string newVendorCode = "V0001";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string queryStr = "SELECT TOP 1 VendorCode FROM Vendors ORDER BY VendorCode DESC";
                SqlCommand cmd = new SqlCommand(queryStr, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string lastVendorCode = reader["VendorCode"]?.ToString();
                    if (!string.IsNullOrEmpty(lastVendorCode))
                    {
                        int lastVendorNum = int.Parse(lastVendorCode.Substring(1));
                        newVendorCode = "V" + (lastVendorNum + 1).ToString("D4");
                    }
                }
            }
            return newVendorCode;
        }
    }
}