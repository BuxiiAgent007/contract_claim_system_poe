using System;
using System.Linq;
using contract_claim_system.Data;
using contract_claim_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace contract_claim_system.Controllers
{
    [Authorize(Roles = "Manager,Admin")]
    public class ManagerController : Controller
    {
        private readonly IClaimRepository _repo;

        public ManagerController(IClaimRepository repo)
        {
            _repo = repo;
        }

        // -----------------------------------------------
        // 1️⃣ Manager Dashboard
        // -----------------------------------------------
        public IActionResult Dashboard()
        {
            try
            {
                var allClaims = _repo.GetAllClaims();

                ViewBag.VerifiedCount = allClaims.Count(c => c.claim_status == "Verified");
                ViewBag.ApprovedCount = allClaims.Count(c => c.claim_status == "Approved");
                ViewBag.TotalAmount = allClaims.Where(c => c.claim_status == "Approved")
                                             .Sum(c => c.number_of_hours * c.amount_of_rate);

                return View(allClaims);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading dashboard: {ex.Message}";
                return View(Enumerable.Empty<Claim>());
            }
        }

        // -----------------------------------------------
        // 2️⃣ Approve Claims View
        // -----------------------------------------------
        public IActionResult ApproveClaims()
        {
            try
            {
                var verifiedClaims = _repo.GetAllClaims()
                    .Where(c => c.claim_status == "Verified")
                    .OrderBy(c => c.verified_date)
                    .ToList();

                return View(verifiedClaims);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading claims: {ex.Message}";
                return View(Enumerable.Empty<Claim>());
            }
        }

        // -----------------------------------------------
        // 3️⃣ Approve a Claim
        // -----------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveClaim(int id)
        {
            try
            {
                var claim = _repo.GetClaimById(id);
                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(ApproveClaims));
                }

                // Check if claim requires special approval
                decimal totalAmount = claim.number_of_hours * claim.amount_of_rate;
                string approvalType = totalAmount > 10000 ? "Special Approval" : "Standard Approval";

                // Update claim with approval details
                claim.claim_status = "Approved";
                claim.approved_by = User.Identity?.Name ?? "Manager";
                claim.approved_date = DateTime.Now;

                bool success = _repo.UpdateClaim(claim);
                if (!success)
                {
                    TempData["Error"] = "Failed to update claim status.";
                    return RedirectToAction(nameof(ApproveClaims));
                }

                TempData["Success"] = $"Claim #{id} approved ({approvalType}) - Amount: R{totalAmount:N2}";
                return RedirectToAction(nameof(ApproveClaims));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error approving claim: {ex.Message}";
                return RedirectToAction(nameof(ApproveClaims));
            }
        }

        // -----------------------------------------------
        // 4️⃣ Reject a Claim
        // -----------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectClaim(int id, string rejectionReason)
        {
            try
            {
                var claim = _repo.GetClaimById(id);
                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(ApproveClaims));
                }

                if (string.IsNullOrEmpty(rejectionReason))
                {
                    TempData["Error"] = "Rejection reason is required.";
                    return RedirectToAction(nameof(ApproveClaims));
                }

                // Update claim with rejection details
                claim.claim_status = "Rejected";
                claim.rejection_reason = rejectionReason;
                claim.approved_by = User.Identity?.Name ?? "Manager";
                claim.approved_date = DateTime.Now;

                bool success = _repo.UpdateClaim(claim);
                if (!success)
                {
                    TempData["Error"] = "Failed to update claim status.";
                    return RedirectToAction(nameof(ApproveClaims));
                }

                TempData["Success"] = $"Claim #{id} has been rejected.";
                return RedirectToAction(nameof(ApproveClaims));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error rejecting claim: {ex.Message}";
                return RedirectToAction(nameof(ApproveClaims));
            }
        }

        // -----------------------------------------------
        // 5️⃣ View Approved Claims
        // -----------------------------------------------
        public IActionResult ApprovedClaims()
        {
            try
            {
                var approvedClaims = _repo.GetAllClaims()
                    .Where(c => c.claim_status == "Approved")
                    .OrderByDescending(c => c.approved_date)
                    .ToList();

                return View(approvedClaims);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading approved claims: {ex.Message}";
                return View(Enumerable.Empty<Claim>());
            }
        }

        // -----------------------------------------------
        // 6️⃣ Financial Overview
        // -----------------------------------------------
        public IActionResult FinancialOverview()
        {
            try
            {
                var allClaims = _repo.GetAllClaims();
                var approvedClaims = allClaims.Where(c => c.claim_status == "Approved");

                var financials = new
                {
                    TotalApprovedAmount = approvedClaims.Sum(c => c.number_of_hours * c.amount_of_rate),
                    ApprovedCount = approvedClaims.Count(),
                    AverageAmount = approvedClaims.Any() ? approvedClaims.Average(c => c.number_of_hours * c.amount_of_rate) : 0,
                    MonthlyBreakdown = approvedClaims.GroupBy(c => new { c.creating_date.Year, c.creating_date.Month })
                                                   .Select(g => new
                                                   {
                                                       Month = $"{g.Key.Year}-{g.Key.Month:00}",
                                                       Amount = g.Sum(c => c.number_of_hours * c.amount_of_rate),
                                                       Count = g.Count()
                                                   })
                                                   .OrderBy(x => x.Month)
                };

                return View(financials);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading financial overview: {ex.Message}";
                return View();
            }
        }
    }
}