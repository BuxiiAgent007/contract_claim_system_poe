using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using contract_claim_system.Data;
using contract_claim_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace contract_claim_system.Controllers
{
    [Authorize(Roles = "Admin,HR,Manager,Coordinator")]
    public class AdminController : Controller
    {
        private readonly IClaimRepository _claimRepo;
        private readonly string _connectionString;

        public AdminController(IClaimRepository claimRepo, IConfiguration config)
        {
            _claimRepo = claimRepo;
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        // Dashboard
        public IActionResult Dashboard()
        {
            var stats = new AdminDashboardStats();

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();

                // Get statistics
                stats.TotalClaims = GetCount(con, "SELECT COUNT(*) FROM Claims");
                stats.PendingClaims = GetCount(con, "SELECT COUNT(*) FROM Claims WHERE claim_status = 'Pending'");
                stats.ApprovedClaims = GetCount(con, "SELECT COUNT(*) FROM Claims WHERE claim_status = 'Approved'");
                stats.TotalUsers = GetCount(con, "SELECT COUNT(*) FROM Users");
                stats.RecentClaims = GetRecentClaims(con);
            }

            return View(stats);
        }

        // User Management
        public IActionResult Users()
        {
            var users = new List<User>();

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var query = "SELECT * FROM Users ORDER BY surname, full_names";

                using (var cmd = new SqlCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(MapUserReader(reader));
                    }
                }
            }

            return View(users);
        }

        [HttpGet]
        public IActionResult AddUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(User user, string password, string confirmPassword)
        {
            if (!ModelState.IsValid)
                return View(user);

            try
            {
                ValidatePassword(password, confirmPassword);

                var hashedPassword = HashPassword(password);
                CreateUser(user, hashedPassword);

                TempData["Success"] = $"User {user.full_names} {user.surname} added successfully.";
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error adding user: {ex.Message}");
                return View(user);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                if (UserHasClaims(id))
                {
                    TempData["Error"] = "Cannot delete user with existing claims. Archive instead.";
                    return RedirectToAction(nameof(Users));
                }

                DeleteUserFromDatabase(id);
                TempData["Success"] = "User deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting user: {ex.Message}";
            }

            return RedirectToAction(nameof(Users));
        }

        // Claims Management
        public IActionResult ManageClaims(string status = "")
        {
            var claims = _claimRepo.GetAllClaims();
            ViewBag.CurrentFilter = status;
            return View(claims);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkUpdateStatus(int[] claimIds, string status)
        {
            if (claimIds == null || claimIds.Length == 0)
            {
                TempData["Error"] = "No claims selected.";
                return RedirectToAction(nameof(ManageClaims));
            }

            try
            {
                var successCount = UpdateClaimStatuses(claimIds, status);
                TempData["Success"] = $"Updated {successCount} claims to '{status}'.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating claims: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageClaims));
        }

        // Financial Reports
        public IActionResult Financials()
        {
            var financials = new FinancialOverview();

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                financials.TotalApprovedAmount = GetTotalApprovedAmount(con);
                financials.AmountByFaculty = GetAmountByFaculty(con);
                financials.MonthlyBreakdown = GetMonthlyBreakdown(con);
            }

            return View(financials);
        }

        // Private Helper Methods
        private int GetCount(SqlConnection con, string query)
        {
            using (var cmd = new SqlCommand(query, con))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private List<Claim> GetRecentClaims(SqlConnection con)
        {
            var claims = new List<Claim>();
            var query = "SELECT TOP 5 * FROM Claims ORDER BY creating_date DESC";

            using (var cmd = new SqlCommand(query, con))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    claims.Add(MapClaimReader(reader));
                }
            }

            return claims;
        }

        private void ValidatePassword(string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password is required.");

            if (password != confirmPassword)
                throw new ArgumentException("Passwords do not match.");

            if (password.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters long.");
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private void CreateUser(User user, string hashedPassword)
        {
            const string query = @"
                INSERT INTO Users (full_names, surname, email, role, gender, password, date)
                VALUES (@full_names, @surname, @email, @role, @gender, @password, @date)";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@full_names", user.full_names ?? "");
                cmd.Parameters.AddWithValue("@surname", user.surname ?? "");
                cmd.Parameters.AddWithValue("@email", user.email ?? "");
                cmd.Parameters.AddWithValue("@role", user.role ?? "Lecturer");
                cmd.Parameters.AddWithValue("@gender", user.gender ?? "");
                cmd.Parameters.AddWithValue("@password", hashedPassword);
                cmd.Parameters.AddWithValue("@date", DateTime.Now);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private bool UserHasClaims(int userId)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var query = "SELECT COUNT(*) FROM Claims WHERE lecturerID = @id";

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        private void DeleteUserFromDatabase(int userId)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var query = "DELETE FROM Users WHERE userID = @id";

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private int UpdateClaimStatuses(int[] claimIds, string status)
        {
            var successCount = 0;

            foreach (var id in claimIds)
            {
                if (_claimRepo.UpdateClaimStatus(id, status))
                    successCount++;
            }

            return successCount;
        }

        private decimal GetTotalApprovedAmount(SqlConnection con)
        {
            var query = "SELECT SUM(number_of_hours * amount_of_rate) FROM Claims WHERE claim_status = 'Approved'";

            using (var cmd = new SqlCommand(query, con))
            {
                var result = cmd.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
        }

        private Dictionary<string, decimal> GetAmountByFaculty(SqlConnection con)
        {
            var amountByFaculty = new Dictionary<string, decimal>();
            var query = @"
                SELECT faculty_name, SUM(number_of_hours * amount_of_rate) as total
                FROM Claims 
                WHERE claim_status = 'Approved'
                GROUP BY faculty_name";

            using (var cmd = new SqlCommand(query, con))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var faculty = reader["faculty_name"]?.ToString() ?? "Unknown";
                    var amount = Convert.ToDecimal(reader["total"]);
                    amountByFaculty[faculty] = amount;
                }
            }

            return amountByFaculty;
        }

        private Dictionary<string, decimal> GetMonthlyBreakdown(SqlConnection con)
        {
            var monthlyBreakdown = new Dictionary<string, decimal>();
            var query = @"
                SELECT FORMAT(creating_date, 'yyyy-MM') as month, 
                       SUM(number_of_hours * amount_of_rate) as total
                FROM Claims 
                WHERE claim_status = 'Approved'
                GROUP BY FORMAT(creating_date, 'yyyy-MM')
                ORDER BY month";

            using (var cmd = new SqlCommand(query, con))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var month = reader["month"]?.ToString();
                    var amount = Convert.ToDecimal(reader["total"]);
                    monthlyBreakdown[month] = amount;
                }
            }

            return monthlyBreakdown;
        }

        private Claim MapClaimReader(SqlDataReader reader)
        {
            return new Claim
            {
                claimID = Convert.ToInt32(reader["claimID"]),
                number_of_sessions = Convert.ToInt32(reader["number_of_sessions"]),
                number_of_hours = Convert.ToInt32(reader["number_of_hours"]),
                amount_of_rate = Convert.ToInt32(reader["amount_of_rate"]),
                module_name = reader["module_name"]?.ToString(),
                faculty_name = reader["faculty_name"]?.ToString(),
                supporting_documents = reader["supporting_documents"]?.ToString(),
                claim_status = reader["claim_status"]?.ToString(),
                creating_date = Convert.ToDateTime(reader["creating_date"]),
                lecturerID = Convert.ToInt32(reader["lecturerID"])
            };
        }

        private User MapUserReader(SqlDataReader reader)
        {
            return new User
            {
                userID = Convert.ToInt32(reader["userID"]),
                full_names = reader["full_names"]?.ToString(),
                surname = reader["surname"]?.ToString(),
                email = reader["email"]?.ToString(),
                role = reader["role"]?.ToString(),
                gender = reader["gender"]?.ToString(),
                date = Convert.ToDateTime(reader["date"])
            };
        }
    }

    // Support classes
    public class AdminDashboardStats
    {
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int TotalUsers { get; set; }
        public List<Claim> RecentClaims { get; set; }
    }

    public class FinancialOverview
    {
        public decimal TotalApprovedAmount { get; set; }
        public Dictionary<string, decimal> AmountByFaculty { get; set; }
        public Dictionary<string, decimal> MonthlyBreakdown { get; set; }
    }
}