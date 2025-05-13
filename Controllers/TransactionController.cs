using ITHelpDesk_Updated.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace ITHelpDesk_Updated.Controllers
{
    public class TransactionController : Controller
    {
        private readonly string connectionString = "Data Source=192.168.0.75;Initial Catalog=ITHD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";
        private readonly string hrdConnectionString = "Data Source=192.168.0.75;Initial Catalog=HRD;User ID=sa;Password=worldcall;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

        [HttpGet]
        public IActionResult CreateTransaction()
        {
            string transactionCode = GenerateTransactionCode();
            ViewData["TransactionCode"] = transactionCode;

            ViewData["Vendors"] = GetVendors();

            ViewData["Item"] = GetItems();

            ViewData["Transactions"] = GetTransactions();

            return View("~/Views/ITHelpDesk/CreateTransaction.cshtml");
        }

        [HttpPost]
        public IActionResult CreateTransaction(Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string insertQuery = @"
                        INSERT INTO Transactions (TransactionCode, VendorName, ItemCode, ItemType, ItemName, ItemCategory, ItemStorage, DateOfPurchase, Price, ReceivedQuantity)
                        VALUES (@TransactionCode, @VendorName, @ItemCode, @ItemType, @ItemName, @ItemCategory, @ItemStorage, @DateOfPurchase, @Price, @ReceivedQuantity)";

                    SqlCommand cmd = new SqlCommand(insertQuery, connection);

                    cmd.Parameters.AddWithValue("@TransactionCode", transaction.TransactionCode);
                    cmd.Parameters.AddWithValue("@VendorName", transaction.VendorName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ItemCode", transaction.ItemCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ItemType", transaction.ItemType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ItemName", transaction.ItemName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ItemCategory", transaction.ItemCategory ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ItemStorage", transaction.ItemStorage ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateOfPurchase", transaction.DateOfPurchase);
                    cmd.Parameters.AddWithValue("@Price", transaction.Price);
                    cmd.Parameters.AddWithValue("@ReceivedQuantity", transaction.ReceivedQuantity);

                    cmd.ExecuteNonQuery();
                }

                return RedirectToAction("CreateTransaction");
            }

            ViewData["TransactionCode"] = transaction.TransactionCode;
            ViewData["Vendors"] = GetVendors();
            ViewData["Item"] = GetItems();
            ViewData["Transactions"] = GetTransactions();
            return View("~/Views/ITHelpDesk/CreateTransaction.cshtml", transaction);
        }

        private string GenerateTransactionCode()
        {
            int nextNumber = 1;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT TransactionCode FROM Transactions WHERE TransactionCode LIKE 'T[0-9][0-9][0-9][0-9]'";
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                List<int> numbers = new List<int>();
                while (reader.Read())
                {
                    string code = reader["TransactionCode"] as string;
                    if (code != null && code.StartsWith("T") && code.Length == 5)
                    {
                        if (int.TryParse(code.Substring(1), out int number))
                        {
                            numbers.Add(number);
                        }
                    }
                }

                if (numbers.Any())
                {
                    nextNumber = numbers.Max() + 1;
                }
            }
            return $"T{nextNumber:D4}";
        }

        private List<SelectListItem> GetVendors()
        {
            List<SelectListItem> vendors = new List<SelectListItem>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT VendorName FROM Vendors";
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    vendors.Add(new SelectListItem
                    {
                        Value = reader["VendorName"].ToString(),
                        Text = reader["VendorName"].ToString()
                    });
                }
            }
            return vendors;
        }

        private List<SelectListItem> GetItems()
        {
            List<SelectListItem> items = new List<SelectListItem>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ItemCode, ItemName FROM Item";
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    items.Add(new SelectListItem
                    {
                        Value = reader["ItemCode"].ToString(),
                        Text = reader["ItemName"].ToString()
                    });
                }
            }
            return items;
        }

        [HttpGet]
        public JsonResult GetItemDetails(string itemCode)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ItemType, ItemName, ItemCategory, ItemStorage FROM Item WHERE ItemCode = @ItemCode";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var result = new
                    {
                        itemType = reader["ItemType"] as string,
                        itemName = reader["ItemName"] as string,
                        itemCategory = reader["ItemCategory"] as string,
                        itemStorage = reader["ItemStorage"] as string
                    };
                    return Json(result);
                }
            }
            return Json(null);
        }

        private List<Transaction> GetTransactions()
        {
            List<Transaction> transactions = new List<Transaction>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string queryStr = "SELECT * FROM Transactions";
                SqlCommand cmd = new SqlCommand(queryStr, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    transactions.Add(new Transaction
                    {
                        TransactionCode = reader["TransactionCode"] as string,
                        VendorName = reader["VendorName"] as string,
                        ItemCode = reader["ItemCode"] as string,
                        ItemType = reader["ItemType"] as string,
                        ItemName = reader["ItemName"] as string,
                        ItemCategory = reader["ItemCategory"] as string,
                        ItemStorage = reader["ItemStorage"] as string,
                        DateOfPurchase = reader["DateOfPurchase"] as DateTime? ?? DateTime.MinValue,
                        Price = reader["Price"] as decimal? ?? 0,
                        ReceivedQuantity = reader["ReceivedQuantity"] as int? ?? 0
                    });
                }
            }
            return transactions;
        }

        [HttpGet]
        public JsonResult GetEmployeeDetails(string issueTo, string searchTerm = "")
        {
            List<Employee> employees = new List<Employee>();
            if (issueTo == "Staff")
            {
                employees = SearchStaffDetails(searchTerm);
            }
            else if (issueTo == "Director")
            {
                employees = new List<Employee>
                {
                    new Employee { FullName = "Ahsan Akbar Khan" },
                    new Employee { FullName = "Dr. Muhammad Shafiq Khan" }
                };
            }
            else if (issueTo == "CEO")
            {
                employees = new List<Employee> { new Employee { FullName = "Saad Akbar Khan" } };
            }
            else if (issueTo == "GM HR & Operations")
            {
                employees = new List<Employee> { new Employee { FullName = "Waqas Shujait" } };
            }
            else if (issueTo == "IT Process")
            {
                employees = new List<Employee> { new Employee { FullName = "None" } };
            }
            else if (issueTo == "Server Room")
            {
                employees = SearchServerRoomDetails(searchTerm);
            }
            return Json(employees);
        }

        private List<Employee> SearchStaffDetails(string searchTerm = "")
        {
            List<Employee> staff = new List<Employee>();
            using (SqlConnection connection = new SqlConnection(hrdConnectionString))
            {
                connection.Open();
                string query = @"SELECT USR_NAME, USR_FULL_NAME 
                               FROM HRD_SEC_USR_USERS 
                               WHERE USR_NAME IS NOT NULL 
                               AND (USR_FULL_NAME LIKE @SearchTerm OR USR_NAME LIKE @SearchTerm)";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    staff.Add(new Employee
                    {
                        UserName = reader["USR_NAME"] as string,
                        FullName = reader["USR_FULL_NAME"] as string
                    });
                }
            }
            return staff;
        }

        private List<Employee> SearchServerRoomDetails(string searchTerm = "")
        {
            List<string> serverRoomUsers = new List<string> { "4357", "1078", "3961", "4762", "3936", "4654", "4999", "1042" };
            List<Employee> serverRoomEmployees = new List<Employee>();
            using (SqlConnection connection = new SqlConnection(hrdConnectionString))
            {
                connection.Open();
                foreach (var usrName in serverRoomUsers)
                {
                    string query = @"SELECT USR_NAME, USR_FULL_NAME 
                                   FROM HRD_SEC_USR_USERS 
                                   WHERE USR_NAME = @USR_NAME 
                                   AND (USR_FULL_NAME LIKE @SearchTerm OR USR_NAME LIKE @SearchTerm)";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@USR_NAME", usrName);
                    cmd.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            serverRoomEmployees.Add(new Employee
                            {
                                UserName = reader["USR_NAME"] as string,
                                FullName = reader["USR_FULL_NAME"] as string
                            });
                        }
                    }
                }
            }
            return serverRoomEmployees;
        }

        [HttpGet]
        public IActionResult CreateTransactionIssuance()
        {
            List<Transaction> transactions = GetTransactions();
            ViewData["Transactions"] = transactions;
            List<TransactionIssuance> transactionIssuances = GetTransactionIssuances();
            ViewData["TransactionIssuance"] = transactionIssuances;
            return View("~/Views/ITHelpDesk/CreateTransactionIssuance.cshtml");
        }

        [HttpPost]
        public IActionResult CreateTransactionIssuance(TransactionIssuance transactionIssuance)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string insertQuery = @"
                INSERT INTO TransactionIssuance (IssueTo, EmployeeDetails, TransactionCode, ItemType, ItemName, ItemCategory, ItemStorage, DateOfIssue, IssuedQuantity, Stock, Description) 
                VALUES (@IssueTo, @EmployeeDetails, @TransactionCode, @ItemType, @ItemName, @ItemCategory, @ItemStorage, @DateOfIssue, @IssuedQuantity, @Stock, @Description)";
                SqlCommand cmd = new SqlCommand(insertQuery, connection);
                cmd.Parameters.AddWithValue("@IssueTo", transactionIssuance.IssueTo);
                cmd.Parameters.AddWithValue("@EmployeeDetails", transactionIssuance.EmployeeDetails);
                cmd.Parameters.AddWithValue("@TransactionCode", transactionIssuance.TransactionCode);
                cmd.Parameters.AddWithValue("@ItemType", transactionIssuance.ItemType);
                cmd.Parameters.AddWithValue("@ItemName", transactionIssuance.ItemName);
                cmd.Parameters.AddWithValue("@ItemCategory", transactionIssuance.ItemCategory);
                cmd.Parameters.AddWithValue("@ItemStorage", transactionIssuance.ItemStorage);
                cmd.Parameters.AddWithValue("@DateOfIssue", transactionIssuance.DateOfIssue);
                cmd.Parameters.AddWithValue("@IssuedQuantity", transactionIssuance.IssuedQuantity);
                cmd.Parameters.AddWithValue("@Stock", transactionIssuance.Stock);
                cmd.Parameters.AddWithValue("@Description", transactionIssuance.Description);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("CreateTransactionIssuance");
        }

        [HttpGet]
        public JsonResult GetTransactionDetails(string transactionCode)
        {
            Transaction transaction = GetTransactionByCode(transactionCode);
            if (transaction != null)
            {
                int totalIssued = 0;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT SUM(IssuedQuantity) FROM TransactionIssuance WHERE TransactionCode = @TransactionCode";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@TransactionCode", transactionCode);
                    var queryResult = cmd.ExecuteScalar();
                    if (queryResult != DBNull.Value)
                    {
                        totalIssued = Convert.ToInt32(queryResult);
                    }
                }
                int remainingStock = transaction.ReceivedQuantity - totalIssued;
                var result = new
                {
                    ItemType = transaction.ItemType,
                    ItemName = transaction.ItemName,
                    ItemCategory = transaction.ItemCategory,
                    ItemStorage = transaction.ItemStorage,
                    Stock = remainingStock
                };
                return Json(result);
            }
            return Json(null);
        }

        private List<TransactionIssuance> GetTransactionIssuances()
        {
            List<TransactionIssuance> transactionIssuances = new List<TransactionIssuance>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM TransactionIssuance";
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    transactionIssuances.Add(new TransactionIssuance
                    {
                        TransactionCode = reader["TransactionCode"] as string,
                        IssueTo = reader["IssueTo"] as string,
                        EmployeeDetails = reader["EmployeeDetails"] as string,
                        ItemType = reader["ItemType"] as string,
                        ItemName = reader["ItemName"] as string,
                        ItemCategory = reader["ItemCategory"] as string,
                        ItemStorage = reader["ItemStorage"] as string,
                        DateOfIssue = reader["DateOfIssue"] as DateTime? ?? DateTime.MinValue,
                        IssuedQuantity = reader["IssuedQuantity"] as int? ?? 0,
                        Stock = reader["Stock"] as int? ?? 0,
                        Description = reader["Description"] as string
                    });
                }
            }
            return transactionIssuances;
        }

        private Transaction GetTransactionByCode(string transactionCode)
        {
            Transaction transaction = null;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Transactions WHERE TransactionCode = @TransactionCode";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@TransactionCode", transactionCode);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    transaction = new Transaction
                    {
                        TransactionCode = reader["TransactionCode"] as string,
                        ItemType = reader["ItemType"] as string,
                        ItemName = reader["ItemName"] as string,
                        ItemCategory = reader["ItemCategory"] as string,
                        ItemStorage = reader["ItemStorage"] as string,
                        ReceivedQuantity = reader["ReceivedQuantity"] as int? ?? 0
                    };
                }
            }
            return transaction;
        }
    }
}