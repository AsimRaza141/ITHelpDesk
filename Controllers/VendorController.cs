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

            string vendorCode = GetNextVendorCode();
            ViewData["VendorCode"] = vendorCode;

            // Fetch the list of submitted vendors
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
                        VendorCode = reader["VendorCode"] as string,
                        VendorName = reader["VendorName"] as string,
                        VendorNTN = reader["VendorNTN"] as string,
                        VendorCNIC = reader["VendorCNIC"] as string,
                        VendorContactNo = reader["VendorContactNo"] as string,
                        VendorContactPerson = reader["VendorContactPerson"] as string,
                    });
                }
            }

            // Pass the list of vendors to the view
            ViewData["Vendors"] = vendors;

            return View("~/Views/ITHelpDesk/CreateVendor.cshtml");
        }

        // Helper method to get the next VendorCode
        private string GetNextVendorCode()
        {
            string newVendorCode = "V0001";  // Default fallback VendorCode

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string queryStr = "SELECT TOP 1 VendorCode FROM Vendors ORDER BY VendorCode DESC";
                SqlCommand cmd = new SqlCommand(queryStr, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string lastVendorCode = reader["VendorCode"] as string;

                    if (!string.IsNullOrEmpty(lastVendorCode))
                    {
                        int lastVendorNum = int.Parse(lastVendorCode.Substring(1)); // Remove "V" prefix
                        newVendorCode = "V" + (lastVendorNum + 1).ToString("D4");
                    }
                }
            }

            return newVendorCode;
        }

        [HttpPost]
        public IActionResult CreateVendor(Vendor vendor)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string insertQuery = "INSERT INTO Vendors (VendorCode, VendorName, VendorNTN, VendorCNIC, VendorContactNo, VendorContactPerson) VALUES (@VendorCode, @VendorName, @VendorNTN, @VendorCNIC, @VendorContactNo, @VendorContactPerson)";

                SqlCommand cmd = new SqlCommand(insertQuery, connection);

                // Add parameters for VendorCode and VendorName and new fields
                cmd.Parameters.AddWithValue("@VendorCode", vendor.VendorCode);
                cmd.Parameters.AddWithValue("@VendorName", vendor.VendorName);
                cmd.Parameters.AddWithValue("@VendorNTN", vendor.VendorNTN);
                cmd.Parameters.AddWithValue("@VendorCNIC", vendor.VendorCNIC);
                cmd.Parameters.AddWithValue("@VendorContactNo", vendor.VendorContactNo);
                cmd.Parameters.AddWithValue("@VendorContactPerson", vendor.VendorContactPerson);

                cmd.ExecuteNonQuery();
            }

            // Redirect to the same form after submission to refresh the list of vendors
            return RedirectToAction("CreateVendor");
        }
    }
}
