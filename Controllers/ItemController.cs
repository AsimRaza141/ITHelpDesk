using ITHelpDesk_Updated.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ITHelpDesk_Updated.Controllers
{
    [NoCache]
    public class ItemController : Controller
    {
        private readonly string connectionString = "Data Source=192.168.0.75;Initial Catalog=ITHD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";
        private readonly string connectionStringHRD = "Data Source=192.168.0.75;Initial Catalog=HRD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

        // Action to display the Create Item form
        [HttpGet]
        public IActionResult CreateItem()
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

            // Fetch the list of items from the database
            List<Item> items = GetAllItems();

            // Create the model and pass the items list
            var model = new ItemViewModel
            {
                Items = items,
                CurrentItem = new Item()
            };

            return View("~/Views/ITHelpDesk/CreateItem.cshtml", model);
        }

        // Action to handle form submission for creating a new Item
        [HttpPost]
        public IActionResult CreateItem(ItemViewModel model)
        {
            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            string USR_FULL_NAME = HttpContext.Session.GetString("USR_FULL_NAME");

            // Generate ItemCode
            string newItemCode = GenerateNewItemCode();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string insertQuery = "INSERT INTO Item (ItemCode, ItemName, ItemCategory, ItemType, ItemStorage, ItemUsedFor, ItemSection, USR_NAME, USR_FULL_NAME) " +
                                     "VALUES (@ItemCode, @ItemName, @ItemCategory, @ItemType, @ItemStorage, @ItemUsedFor, @ItemSection, @USR_NAME, @USR_FULL_NAME)";
                SqlCommand cmd = new SqlCommand(insertQuery, connection);

                cmd.Parameters.AddWithValue("@ItemCode", newItemCode);
                cmd.Parameters.AddWithValue("@ItemName", model.CurrentItem.ItemName ?? "Untitled");
                cmd.Parameters.AddWithValue("@ItemCategory", model.CurrentItem.ItemCategory ?? "Uncategorized");
                cmd.Parameters.AddWithValue("@ItemType", model.CurrentItem.ItemType ?? "General");
                cmd.Parameters.AddWithValue("@ItemStorage", model.CurrentItem.ItemStorage ?? "Unknown");
                cmd.Parameters.AddWithValue("@ItemUsedFor", model.CurrentItem.ItemUsedFor ?? "Not specified");
                cmd.Parameters.AddWithValue("@ItemSection", model.CurrentItem.ItemSection ?? "Unspecified");
                cmd.Parameters.AddWithValue("@USR_NAME", USR_NAME);
                cmd.Parameters.AddWithValue("@USR_FULL_NAME", USR_FULL_NAME ?? "Unknown");

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("CreateItem");
        }



        // Helper method to fetch user details from HRD database
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

        // Method to fetch all items from the database
        private List<Item> GetAllItems()
        {
            List<Item> items = new List<Item>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ItemCode, ItemName, ItemCategory, ItemType, ItemStorage, ItemUsedFor, ItemSection FROM Item";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    items.Add(new Item
                    {
                        ItemCode = reader["ItemCode"] as string,
                        ItemName = reader["ItemName"] as string,
                        ItemCategory = reader["ItemCategory"] as string,
                        ItemType = reader["ItemType"] as string,
                        ItemStorage = reader["ItemStorage"] as string,
                        ItemUsedFor = reader["ItemUsedFor"] as string,
                        ItemSection = reader["ItemSection"] as string
                    });
                }
            }

            return items;
        }

        // Generate a new ItemCode
        private string GenerateNewItemCode()
        {
            string newItemCode = "I00001";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT TOP 1 ItemCode FROM Item ORDER BY ItemCode DESC";
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string lastItemCode = reader["ItemCode"] as string;
                    if (!string.IsNullOrEmpty(lastItemCode))
                    {
                        int lastItemNum = int.Parse(lastItemCode.Substring(1));
                        newItemCode = "I" + (lastItemNum + 1).ToString("D5");
                    }
                }
            }

            return newItemCode;
        }
    }
}
