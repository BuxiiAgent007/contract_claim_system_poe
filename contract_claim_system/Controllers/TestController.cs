using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace contract_claim_system.Controllers
{
    public class TestController : Controller
    {

        public IActionResult ShowUsers()
        {
            var users = new List<dynamic>();
            string connectionString = "Server=(localdb)\\claim_system;Database=claims_database;Integrated Security=true;";

            using (var con = new SqlConnection(connectionString))
            {
                con.Open();
                var cmd = new SqlCommand("SELECT userID, full_names, surname, email FROM Users ORDER BY userID", con);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new
                        {
                            UserID = reader["userID"],
                            Name = $"{reader["full_names"]} {reader["surname"]}",
                            Email = reader["email"]
                        });
                    }
                }
            }

            return View(users);
        }

        public IActionResult FixDatabase()
        {
            try
            {
                string connectionString = "Server=LabVM1846780\\SQLEXPRESS;Database=claims_database;Integrated Security=true;";

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Create multiple test users
                    var cmd = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM Users WHERE userID = 1)
                BEGIN
                    INSERT INTO Users (full_names, surname, email, role, gender, password, date) 
                    VALUES 
                    ('John', 'Doe', 'john.doe@university.com', 'Lecturer', 'Male', 'password123', GETDATE()),
                    ('Sarah', 'Smith', 'sarah.smith@university.com', 'Lecturer', 'Female', 'password123', GETDATE()),
                    ('Mike', 'Johnson', 'mike.johnson@university.com', 'Lecturer', 'Male', 'password123', GETDATE())
                END", con);

                    int affected = cmd.ExecuteNonQuery();
                    TempData["Success"] = "Test users created! Use IDs: 1, 2, or 3";
                }

                return RedirectToAction("ShowUsers", "Test");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to create users: {ex.Message}";
                return RedirectToAction("ShowUsers", "Test");
            }
        }
    }
}