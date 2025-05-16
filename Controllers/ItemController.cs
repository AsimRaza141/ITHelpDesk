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

            // Fetch the list of items and custom values from the database
            List<Item> items = GetAllItems();
            var customItemNames = GetCustomItemNames();
            var customCategories = GetCustomCategories();
            var customItemTypes = GetCustomItemTypes();
            var customStorages = GetCustomStorages();

            // Create the model and pass the items and custom values
            var model = new ItemViewModel
            {
                Items = items,
                CurrentItem = new Item(),
                CustomItemNames = customItemNames,
                CustomCategories = customCategories,
                CustomItemTypes = customItemTypes,
                CustomStorages = customStorages
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

            string USR_FULL_NAME = HttpContext.Session.GetString("USR_FULL_NAME") ?? "Unknown";

            // Generate ItemCode and assign it to the model before validation
            if (model.CurrentItem.ItemCode == null)
            {
                model.CurrentItem.ItemCode = GenerateNewItemCode();
            }

            // Exclude non-form fields from validation
            ModelState.Remove("CurrentItem.ItemCode"); // ItemCode is generated server-side
            ModelState.Remove("Items"); // Items is for display only
            ModelState.Remove("UserName"); // UserName is not a model property (session-based)

            if (ModelState.IsValid)
            {
                // Insert item into Item table
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string insertQuery = "INSERT INTO Item (ItemCode, ItemName, ItemCategory, ItemType, ItemStorage, ItemUsedFor, ItemSection, USR_NAME, USR_FULL_NAME) " +
                                        "VALUES (@ItemCode, @ItemName, @ItemCategory, @ItemType, @ItemStorage, @ItemUsedFor, @ItemSection, @USR_NAME, @USR_FULL_NAME)";
                    SqlCommand cmd = new SqlCommand(insertQuery, connection);

                    cmd.Parameters.AddWithValue("@ItemCode", model.CurrentItem.ItemCode);
                    cmd.Parameters.AddWithValue("@ItemName", model.CurrentItem.ItemName ?? "Untitled");
                    cmd.Parameters.AddWithValue("@ItemCategory", model.CurrentItem.ItemCategory ?? "Uncategorized");
                    cmd.Parameters.AddWithValue("@ItemType", model.CurrentItem.ItemType ?? "General");
                    cmd.Parameters.AddWithValue("@ItemStorage", model.CurrentItem.ItemStorage ?? "Unknown");
                    cmd.Parameters.AddWithValue("@ItemUsedFor", model.CurrentItem.ItemUsedFor ?? "Not specified");
                    cmd.Parameters.AddWithValue("@ItemSection", model.CurrentItem.ItemSection ?? "Unspecified");
                    cmd.Parameters.AddWithValue("@USR_NAME", USR_NAME ?? "UnknownUser"); // Fallback if session fails
                    cmd.Parameters.AddWithValue("@USR_FULL_NAME", USR_FULL_NAME);

                    cmd.ExecuteNonQuery();
                }

                // Store custom values
                StoreCustomValues(model.CurrentItem);

                return RedirectToAction("CreateItem");
            }

            // If invalid, fetch items and custom values for the view
            model.Items = GetAllItems();
            model.CustomItemNames = GetCustomItemNames();
            model.CustomCategories = GetCustomCategories();
            model.CustomItemTypes = GetCustomItemTypes();
            model.CustomStorages = GetCustomStorages();
            return View("~/Views/ITHelpDesk/CreateItem.cshtml", model);
        }

        // Action to display the Edit Item form
        [HttpGet]
        public IActionResult EditItem(string itemCode)
        {
            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch user info
            var userDetails = FetchUserDetailsFromHRD(USR_NAME);
            ViewData["USR_NAME"] = userDetails.UsrName;
            ViewData["USR_FULL_NAME"] = userDetails.UsrFullName;

            // Fetch the item to edit
            Item item = GetItemByCode(itemCode);
            if (item == null)
            {
                return NotFound();
            }

            // Fetch all items and custom values
            List<Item> items = GetAllItems();
            var customItemNames = GetCustomItemNames();
            var customCategories = GetCustomCategories();
            var customItemTypes = GetCustomItemTypes();
            var customStorages = GetCustomStorages();

            // Create the model
            var model = new ItemViewModel
            {
                Items = items,
                CurrentItem = item,
                CustomItemNames = customItemNames,
                CustomCategories = customCategories,
                CustomItemTypes = customItemTypes,
                CustomStorages = customStorages
            };

            return View("~/Views/ITHelpDesk/CreateItem.cshtml", model);
        }

        // Action to handle form submission for editing an item
        [HttpPost]
        public IActionResult EditItem(ItemViewModel model)
        {
            string USR_NAME = HttpContext.Session.GetString("USR_NAME");

            if (string.IsNullOrEmpty(USR_NAME))
            {
                return RedirectToAction("Login", "Account");
            }

            string USR_FULL_NAME = HttpContext.Session.GetString("USR_FULL_NAME") ?? "Unknown";

            // Exclude non-form fields from validation
            ModelState.Remove("Items"); // Items is for display only
            ModelState.Remove("UserName"); // UserName is not a model property (session-based)

            if (ModelState.IsValid)
            {
                // Update item in Item table
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string updateQuery = "UPDATE Item SET ItemName = @ItemName, ItemCategory = @ItemCategory, ItemType = @ItemType, " +
                                        "ItemStorage = @ItemStorage, ItemUsedFor = @ItemUsedFor, ItemSection = @ItemSection, " +
                                        "USR_NAME = @USR_NAME, USR_FULL_NAME = @USR_FULL_NAME WHERE ItemCode = @ItemCode";
                    SqlCommand cmd = new SqlCommand(updateQuery, connection);

                    cmd.Parameters.AddWithValue("@ItemCode", model.CurrentItem.ItemCode);
                    cmd.Parameters.AddWithValue("@ItemName", model.CurrentItem.ItemName ?? "Untitled");
                    cmd.Parameters.AddWithValue("@ItemCategory", model.CurrentItem.ItemCategory ?? "Uncategorized");
                    cmd.Parameters.AddWithValue("@ItemType", model.CurrentItem.ItemType ?? "General");
                    cmd.Parameters.AddWithValue("@ItemStorage", model.CurrentItem.ItemStorage ?? "Unknown");
                    cmd.Parameters.AddWithValue("@ItemUsedFor", model.CurrentItem.ItemUsedFor ?? "Not specified");
                    cmd.Parameters.AddWithValue("@ItemSection", model.CurrentItem.ItemSection ?? "Unspecified");
                    cmd.Parameters.AddWithValue("@USR_NAME", USR_NAME ?? "UnknownUser"); // Fallback if session fails
                    cmd.Parameters.AddWithValue("@USR_FULL_NAME", USR_FULL_NAME);

                    cmd.ExecuteNonQuery();
                }

                // Store custom values
                StoreCustomValues(model.CurrentItem);

                return RedirectToAction("CreateItem");
            }

            // If invalid, fetch items and custom values
            model.Items = GetAllItems();
            model.CustomItemNames = GetCustomItemNames();
            model.CustomCategories = GetCustomCategories();
            model.CustomItemTypes = GetCustomItemTypes();
            model.CustomStorages = GetCustomStorages();
            return View("~/Views/ITHelpDesk/CreateItem.cshtml", model);
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

        // Method to fetch a single item by ItemCode
        private Item GetItemByCode(string itemCode)
        {
            Item item = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ItemCode, ItemName, ItemCategory, ItemType, ItemStorage, ItemUsedFor, ItemSection FROM Item WHERE ItemCode = @ItemCode";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ItemCode", itemCode);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    item = new Item
                    {
                        ItemCode = reader["ItemCode"] as string,
                        ItemName = reader["ItemName"] as string,
                        ItemCategory = reader["ItemCategory"] as string,
                        ItemType = reader["ItemType"] as string,
                        ItemStorage = reader["ItemStorage"] as string,
                        ItemUsedFor = reader["ItemUsedFor"] as string,
                        ItemSection = reader["ItemSection"] as string
                    };
                }
            }

            return item;
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

        // Fetch custom ItemNames from database
        private Dictionary<string, List<string>> GetCustomItemNames()
        {
            var customItemNames = new Dictionary<string, List<string>>
        {
            { "Main Hardware", new List<string>() },
            { "Accessories", new List<string>() }
        };

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ItemSection, ItemName FROM CustomItemNames";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string section = reader["ItemSection"] as string;
                    string itemName = reader["ItemName"] as string;
                    if (customItemNames.ContainsKey(section) && !customItemNames[section].Contains(itemName))
                    {
                        customItemNames[section].Add(itemName);
                    }
                }
            }

            return customItemNames;
        }

        // Fetch custom Categories from database
        private Dictionary<string, List<string>> GetCustomCategories()
        {
            var customCategories = new Dictionary<string, List<string>>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ItemName, Category FROM CustomCategories";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string itemName = reader["ItemName"] as string;
                    string category = reader["Category"] as string;
                    if (!customCategories.ContainsKey(itemName))
                    {
                        customCategories[itemName] = new List<string>();
                    }
                    if (!customCategories[itemName].Contains(category))
                    {
                        customCategories[itemName].Add(category);
                    }
                }
            }

            return customCategories;
        }

        // Fetch custom ItemTypes from database
        private List<string> GetCustomItemTypes()
        {
            var customItemTypes = new List<string>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ItemType FROM CustomItemTypes";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string itemType = reader["ItemType"] as string;
                    if (!customItemTypes.Contains(itemType))
                    {
                        customItemTypes.Add(itemType);
                    }
                }
            }

            return customItemTypes;
        }

        // Fetch custom Storages from database
        private List<string> GetCustomStorages()
        {
            var customStorages = new List<string>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Storage FROM CustomStorages";
                SqlCommand cmd = new SqlCommand(query, connection);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string storage = reader["Storage"] as string;
                    if (!customStorages.Contains(storage))
                    {
                        customStorages.Add(storage);
                    }
                }
            }

            return customStorages;
        }

        // Store custom values in the database
        private void StoreCustomValues(Item item)
        {
            // Predefined values from CreateItem.cshtml
            var predefinedItems = new Dictionary<string, List<string>>
        {
            {
                "Main Hardware", new List<string>
                {
                    "LCD", "BareBone", "CPU", "Laptop", "Cameras", "DVR/NVR",
                    "Printer", "Scanner", "Toner", "Network Switch", "UPS", "Projector"
                }
            },
            {
                "Accessories", new List<string>
                {
                    "HDD", "RAM", "Motherboard", "LAN Card", "Connectors", "Keyboard",
                    "Mouse", "USB", "UPS Batteries", "Laptop Battery", "Laptop Charger",
                    "Recycle Toner", "Miscellaneous"
                }
            }
        };

            var predefinedCategories = new Dictionary<string, List<string>>
        {
            { "HDD", new List<string> { "SATA", "SSD", "SAS", "SSD Enterprise", "M.2" } },
            { "RAM", new List<string> { "DDR2", "DDR3", "DDR4" } },
            { "Connectors", new List<string> { "D2 Type" } },
            { "Keyboard", new List<string> { "None" } },
            { "Mouse", new List<string> { "None" } },
            { "USB", new List<string> { "None" } },
            { "Cameras", new List<string> { "Analog", "Digital" } }
        };

            var predefinedItemTypes = new List<string> { "New", "Old", "Used", "Refurbished", "Branded", "Not Required" };
            var predefinedStorages = new List<string> { "8 GB", "16 GB", "32 GB", "64 GB", "128 GB", "256 GB", "512 GB", "1 TB", "2 TB", "Not Required" };

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Store custom ItemName
                if (!string.IsNullOrEmpty(item.ItemSection) && !string.IsNullOrEmpty(item.ItemName) &&
                    predefinedItems.ContainsKey(item.ItemSection) && !predefinedItems[item.ItemSection].Contains(item.ItemName))
                {
                    string query = "INSERT INTO CustomItemNames (ItemSection, ItemName) " +
                                   "SELECT @ItemSection, @ItemName " +
                                   "WHERE NOT EXISTS (SELECT 1 FROM CustomItemNames WHERE ItemSection = @ItemSection AND ItemName = @ItemName)";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@ItemSection", item.ItemSection);
                    cmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                    cmd.ExecuteNonQuery();
                }

                // Store custom Category
                if (!string.IsNullOrEmpty(item.ItemName) && !string.IsNullOrEmpty(item.ItemCategory) &&
                    (!predefinedCategories.ContainsKey(item.ItemName) || !predefinedCategories[item.ItemName].Contains(item.ItemCategory)))
                {
                    string query = "INSERT INTO CustomCategories (ItemName, Category) " +
                                   "SELECT @ItemName, @Category " +
                                   "WHERE NOT EXISTS (SELECT 1 FROM CustomCategories WHERE ItemName = @ItemName AND Category = @Category)";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                    cmd.Parameters.AddWithValue("@Category", item.ItemCategory);
                    cmd.ExecuteNonQuery();
                }

                // Store custom ItemType
                if (!string.IsNullOrEmpty(item.ItemType) && !predefinedItemTypes.Contains(item.ItemType))
                {
                    string query = "INSERT INTO CustomItemTypes (ItemType) " +
                                   "SELECT @ItemType " +
                                   "WHERE NOT EXISTS (SELECT 1 FROM CustomItemTypes WHERE ItemType = @ItemType)";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@ItemType", item.ItemType);
                    cmd.ExecuteNonQuery();
                }

                // Store custom Storage
                if (!string.IsNullOrEmpty(item.ItemStorage) && !predefinedStorages.Contains(item.ItemStorage))
                {
                    string query = "INSERT INTO CustomStorages (Storage) " +
                                   "SELECT @Storage " +
                                   "WHERE NOT EXISTS (SELECT 1 FROM CustomStorages WHERE Storage = @Storage)";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@Storage", item.ItemStorage);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}