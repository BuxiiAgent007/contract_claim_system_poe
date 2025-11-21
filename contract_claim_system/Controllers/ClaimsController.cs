using System.Data.SqlClient;
using contract_claim_system.Data;
using contract_claim_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace contract_claim_system.Controllers
{
    [Authorize(Roles = "Lecturer,Admin")]
    public class ClaimsController : Controller
    {
        private readonly IClaimRepository _repo;
        private readonly IConfiguration _config;

        public ClaimsController(IClaimRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        // Public Actions
        public IActionResult Submit()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(IFormCollection form, IFormFile supportingDocument)
        {
            try
            {
                var claim = ParseClaimForm(form, supportingDocument);
                int claimId = _repo.CreateClaim(claim);

                TempData["Success"] = $"Claim submitted successfully! Your claim ID is #{claimId}.";
                return RedirectToAction(nameof(Details), new { id = claimId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error submitting claim: {ex.Message}";
                return View();
            }
        }

        public IActionResult AllClaims(string lecturerIdOrName)
        {
            try
            {
                var claims = _repo.GetAllClaims();

                if (!string.IsNullOrEmpty(lecturerIdOrName) && int.TryParse(lecturerIdOrName, out int lecturerId))
                {
                    claims = claims.Where(c => c.lecturerID == lecturerId).ToList();
                    ViewBag.FilterMessage = $"Filtered by Lecturer ID: {lecturerId}";
                }

                return View(claims);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading claims: {ex.Message}";
                return View(new List<Claim>());
            }
        }

        public IActionResult Pending()
        {
            try
            {
                var pendingClaims = _repo.GetAllClaims()
                    .Where(c => c.claim_status == "Pending")
                    .OrderBy(c => c.creating_date)
                    .ToList();

                return View(pendingClaims);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading pending claims: {ex.Message}";
                return View(new List<Claim>());
            }
        }

        public IActionResult MyClaims(int? lecturerID)
        {
            try
            {
                if (lecturerID == null || lecturerID <= 0)
                    return View("EnterLecturerID");

                var claims = _repo.GetClaimsByLecturer(lecturerID.Value);
                ViewBag.LecturerID = lecturerID.Value;

                return View(claims);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading your claims: {ex.Message}";
                return View(new List<Claim>());
            }
        }

        public IActionResult Details(int id)
        {
            try
            {
                var claim = _repo.GetClaimById(id);
                if (claim == null)
                {
                    TempData["Error"] = $"Claim #{id} not found.";
                    return RedirectToAction(nameof(AllClaims));
                }

                return View(claim);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading claim details: {ex.Message}";
                return RedirectToAction(nameof(AllClaims));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, string status)
        {
            try
            {
                var claim = _repo.GetClaimById(id);
                if (claim == null)
                {
                    TempData["Error"] = $"Claim #{id} not found.";
                    return RedirectToAction(nameof(AllClaims));
                }

                if (!_repo.UpdateClaimStatus(id, status))
                {
                    TempData["Error"] = "Unable to update claim status.";
                    return RedirectToAction(nameof(AllClaims));
                }

                TempData["Success"] = $"Claim #{id} has been {status.ToLower()}.";
                return RedirectToAction(nameof(AllClaims));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating status: {ex.Message}";
                return RedirectToAction(nameof(AllClaims));
            }
        }

        public IActionResult Search(string query)
        {
            try
            {
                var allClaims = _repo.GetAllClaims();
                var results = SearchClaims(allClaims, query);

                ViewBag.SearchQuery = query;
                ViewBag.ResultCount = results.Count();

                return View("AllClaims", results);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error searching claims: {ex.Message}";
                return RedirectToAction(nameof(AllClaims));
            }
        }

        // Debug/Test Methods
        public IActionResult CreateTestClaim()
        {
            try
            {
                var testClaim = CreateSampleClaim();
                int claimId = _repo.CreateClaim(testClaim);

                TempData["Success"] = $"Test claim #{claimId} created successfully!";
                return RedirectToAction(nameof(MyClaims), new { lecturerID = 1 });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to create test claim: {ex.Message}";
                return RedirectToAction(nameof(MyClaims), new { lecturerID = 1 });
            }
        }

        public IActionResult CheckConnection()
        {
            try
            {
                var connectionInfo = GetDatabaseConnectionInfo();
                return Content(connectionInfo);
            }
            catch (Exception ex)
            {
                return Content($"Connection FAILED: {ex.Message}");
            }
        }

        // Private Helper Methods
        private Claim ParseClaimForm(IFormCollection form, IFormFile supportingDocument)
        {
            ValidateFormFields(form, out int sessions, out int hours, out int rate, out int lecturerID);

            return new Claim
            {
                number_of_sessions = sessions,
                number_of_hours = hours,
                amount_of_rate = rate,
                module_name = form["module_name"].ToString(),
                faculty_name = form["faculty_name"].ToString(),
                supporting_documents = HandleFileUpload(supportingDocument),
                claim_status = "Pending",
                creating_date = DateTime.Now,
                lecturerID = lecturerID
            };
        }

        private void ValidateFormFields(IFormCollection form, out int sessions, out int hours, out int rate, out int lecturerID)
        {
            if (!int.TryParse(form["number_of_sessions"], out sessions))
                throw new ArgumentException("Number of sessions must be a valid number.");

            if (!int.TryParse(form["number_of_hours"], out hours))
                throw new ArgumentException("Number of hours must be a valid number.");

            if (!int.TryParse(form["amount_of_rate"], out rate))
                throw new ArgumentException("Hourly rate must be a valid number.");

            if (!int.TryParse(form["lecturerID"], out lecturerID))
                throw new ArgumentException("Lecturer ID must be a valid number.");
        }

        private string HandleFileUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            return $"/uploads/{uniqueFileName}";
        }

        private List<Claim> SearchClaims(IEnumerable<Claim> claims, string query)
        {
            if (string.IsNullOrEmpty(query))
                return claims.ToList();

            if (int.TryParse(query, out int number))
            {
                return claims.Where(c => c.claimID == number || c.lecturerID == number).ToList();
            }

            return claims.Where(c =>
                (c.module_name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                (c.faculty_name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                (c.claim_status?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            ).ToList();
        }

        private Claim CreateSampleClaim()
        {
            return new Claim
            {
                number_of_sessions = 2,
                number_of_hours = 10,
                amount_of_rate = 100,
                module_name = "Test Module",
                faculty_name = "Test Faculty",
                supporting_documents = null,
                claim_status = "Pending",
                creating_date = DateTime.Now,
                lecturerID = 1
            };
        }

        private string GetDatabaseConnectionInfo()
        {
            var connectionString = _config.GetConnectionString("DefaultConnection");
            var serverInfo = "";
            var tableInfo = "";

            using (var con = new SqlConnection(connectionString))
            {
                con.Open();

                // Get server info
                using (var serverCmd = new SqlCommand("SELECT @@SERVERNAME as ServerName, DB_NAME() as DatabaseName", con))
                using (var reader = serverCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        serverInfo = $"Server: {reader["ServerName"]}, Database: {reader["DatabaseName"]}";
                    }
                }

                // Get table counts
                using (var tablesCmd = new SqlCommand(@"
                    SELECT 
                        (SELECT COUNT(*) FROM Users) as UserCount,
                        (SELECT COUNT(*) FROM Claims) as ClaimCount", con))
                using (var reader = tablesCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        tableInfo = $"Users: {reader["UserCount"]}, Claims: {reader["ClaimCount"]}";
                    }
                }
            }

            return $"Connection: {connectionString}\n{serverInfo}\n{tableInfo}";
        }
    }
}