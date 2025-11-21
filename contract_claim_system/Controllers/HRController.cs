using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using contract_claim_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace contract_claim_system.Controllers
{
    [Authorize(Roles = "HR,Admin")]
    public class HRController : Controller
    {
        private readonly string _connectionString;

        public HRController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        // Public Actions
        public IActionResult Dashboard()
        {
            var stats = new HRDashboardStats();

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                stats.ApprovedClaimsCount = GetApprovedClaimsCount(con);
                stats.TotalApprovedAmount = GetTotalApprovedAmount(con);
                stats.RecentApprovedClaims = GetRecentApprovedClaims(con);
            }

            return View(stats);
        }

        public IActionResult GeneratePaymentReport(string period)
        {
            var reportData = GetPaymentReportData(period);
            var csvContent = GenerateCsvReport(reportData);
            var bytes = Encoding.UTF8.GetBytes(csvContent);

            return File(bytes, "text/csv", $"PaymentReport_{DateTime.Now:yyyyMMdd}.csv");
        }

        public IActionResult ManageLecturers()
        {
            var lecturers = GetLecturers();
            return View(lecturers);
        }

        [HttpPost]
        public IActionResult UpdateLecturer(int id, string email, string full_names, string surname)
        {
            try
            {
                var success = UpdateLecturerInfo(id, email, full_names, surname);

                if (success)
                    TempData["Success"] = "Lecturer information updated successfully";
                else
                    TempData["Error"] = "Failed to update lecturer information";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating lecturer: {ex.Message}";
            }

            return RedirectToAction("ManageLecturers");
        }

        // Private Helper Methods
        private int GetApprovedClaimsCount(SqlConnection con)
        {
            var query = "SELECT COUNT(*) FROM Claims WHERE claim_status = 'Approved'";
            using (var cmd = new SqlCommand(query, con))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
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

        private List<dynamic> GetRecentApprovedClaims(SqlConnection con)
        {
            var recentClaims = new List<dynamic>();
            var query = @"
                SELECT c.*, u.full_names, u.surname 
                FROM Claims c 
                LEFT JOIN Users u ON c.lecturerID = u.userID 
                WHERE c.claim_status = 'Approved' 
                ORDER BY c.creating_date DESC";

            using (var cmd = new SqlCommand(query, con))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    recentClaims.Add(new
                    {
                        ClaimID = reader["claimID"],
                        LecturerName = $"{reader["full_names"]} {reader["surname"]}",
                        Module = reader["module_name"],
                        Amount = Convert.ToInt32(reader["number_of_hours"]) * Convert.ToInt32(reader["amount_of_rate"]),
                        Date = Convert.ToDateTime(reader["creating_date"])
                    });
                }
            }

            return recentClaims;
        }

        private List<PaymentReportItem> GetPaymentReportData(string period)
        {
            var reportData = new List<PaymentReportItem>();

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var query = BuildPaymentReportQuery(period);

                using (var cmd = new SqlCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reportData.Add(MapPaymentReportItem(reader));
                    }
                }
            }

            return reportData;
        }

        private string BuildPaymentReportQuery(string period)
        {
            var baseQuery = @"
                SELECT c.claimID, u.full_names, u.surname, u.email, 
                       c.module_name, c.faculty_name, c.number_of_hours, 
                       c.amount_of_rate, (c.number_of_hours * c.amount_of_rate) as total_amount,
                       c.creating_date
                FROM Claims c
                LEFT JOIN Users u ON c.lecturerID = u.userID
                WHERE c.claim_status = 'Approved'";

            return period switch
            {
                "monthly" => baseQuery + " AND MONTH(c.creating_date) = MONTH(GETDATE()) AND YEAR(c.creating_date) = YEAR(GETDATE())",
                "weekly" => baseQuery + " AND c.creating_date >= DATEADD(DAY, -7, GETDATE())",
                _ => baseQuery
            };
        }

        private PaymentReportItem MapPaymentReportItem(SqlDataReader reader)
        {
            return new PaymentReportItem
            {
                ClaimID = Convert.ToInt32(reader["claimID"]),
                LecturerName = $"{reader["full_names"]} {reader["surname"]}",
                Email = reader["email"]?.ToString(),
                Module = reader["module_name"]?.ToString(),
                Faculty = reader["faculty_name"]?.ToString(),
                Hours = Convert.ToInt32(reader["number_of_hours"]),
                Rate = Convert.ToInt32(reader["amount_of_rate"]),
                TotalAmount = Convert.ToDecimal(reader["total_amount"]),
                Date = Convert.ToDateTime(reader["creating_date"])
            };
        }

        private string GenerateCsvReport(List<PaymentReportItem> reportData)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ClaimID,LecturerName,Email,Module,Faculty,Hours,Rate,TotalAmount,Date");

            foreach (var item in reportData)
            {
                csv.AppendLine($"{item.ClaimID},{item.LecturerName},{item.Email},{item.Module},{item.Faculty},{item.Hours},{item.Rate},{item.TotalAmount},{item.Date:yyyy-MM-dd}");
            }

            return csv.ToString();
        }

        private List<User> GetLecturers()
        {
            var lecturers = new List<User>();

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var query = "SELECT * FROM Users WHERE role = 'Lecturer' ORDER BY surname, full_names";

                using (var cmd = new SqlCommand(query, con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lecturers.Add(MapUserReader(reader));
                    }
                }
            }

            return lecturers;
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

        private bool UpdateLecturerInfo(int id, string email, string full_names, string surname)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var query = @"
                    UPDATE Users 
                    SET email = @email, full_names = @full_names, surname = @surname 
                    WHERE userID = @id";

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@full_names", full_names);
                    cmd.Parameters.AddWithValue("@surname", surname);
                    cmd.Parameters.AddWithValue("@id", id);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }

    // Support Classes
    public class HRDashboardStats
    {
        public int ApprovedClaimsCount { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public List<dynamic> RecentApprovedClaims { get; set; }
    }

    public class PaymentReportItem
    {
        public int ClaimID { get; set; }
        public string LecturerName { get; set; }
        public string Email { get; set; }
        public string Module { get; set; }
        public string Faculty { get; set; }
        public int Hours { get; set; }
        public int Rate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime Date { get; set; }
    }
}