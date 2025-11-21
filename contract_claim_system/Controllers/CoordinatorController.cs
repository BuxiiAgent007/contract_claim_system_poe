using System;
using System.Linq;
using contract_claim_system.Data;
using contract_claim_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace contract_claim_system.Controllers
{
    [Authorize(Roles = "Coordinator,Admin")]
    public class CoordinatorController : Controller
    {
        private readonly IClaimRepository _repo;

        public CoordinatorController(IClaimRepository repo)
        {
            _repo = repo;
        }

        // -----------------------------------------------
        // 1️⃣ Coordinator Dashboard
        // -----------------------------------------------
        public IActionResult Dashboard()
        {
            try
            {
                var allClaims = _repo.GetAllClaims();

                ViewBag.PendingCount = allClaims.Count(c => c.claim_status == "Pending");
                ViewBag.VerifiedCount = allClaims.Count(c => c.claim_status == "Verified");
                ViewBag.TotalClaims = allClaims.Count();

                return View(allClaims);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading dashboard: {ex.Message}";
                return View(Enumerable.Empty<Claim>());
            }
        }

        // -----------------------------------------------
        // 2️⃣ Verify Claims View
        // -----------------------------------------------
        public IActionResult VerifyClaims()
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
                TempData["Error"] = $"Error loading claims: {ex.Message}";
                return View(Enumerable.Empty<Claim>());
            }
        }

        // -----------------------------------------------
        // 3️⃣ Verify a Claim
        // -----------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyClaim(int id)
        {
            try
            {
                var claim = _repo.GetClaimById(id);
                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(VerifyClaims));
                }

                // Update claim with verification details
                claim.claim_status = "Verified";
                claim.verified_by = User.Identity?.Name ?? "Coordinator";
                claim.verified_date = DateTime.Now;

                bool success = _repo.UpdateClaim(claim);
                if (!success)
                {
                    TempData["Error"] = "Failed to update claim status.";
                    return RedirectToAction(nameof(VerifyClaims));
                }

                TempData["Success"] = $"Claim #{id} has been verified successfully.";
                return RedirectToAction(nameof(VerifyClaims));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error verifying claim: {ex.Message}";
                return RedirectToAction(nameof(VerifyClaims));
            }
        }

        // -----------------------------------------------
        // 4️⃣ Query a Claim
        // -----------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QueryClaim(int id, string queryReason)
        {
            try
            {
                var claim = _repo.GetClaimById(id);
                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction(nameof(VerifyClaims));
                }

                if (string.IsNullOrEmpty(queryReason))
                {
                    TempData["Error"] = "Query reason is required.";
                    return RedirectToAction(nameof(VerifyClaims));
                }

                // Update claim with query details
                claim.claim_status = "Query";
                claim.rejection_reason = queryReason;
                claim.verified_by = User.Identity?.Name ?? "Coordinator";
                claim.verified_date = DateTime.Now;

                bool success = _repo.UpdateClaim(claim);
                if (!success)
                {
                    TempData["Error"] = "Failed to update claim status.";
                    return RedirectToAction(nameof(VerifyClaims));
                }

                TempData["Success"] = $"Claim #{id} has been queried. Lecturer will be notified.";
                return RedirectToAction(nameof(VerifyClaims));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error querying claim: {ex.Message}";
                return RedirectToAction(nameof(VerifyClaims));
            }
        }

        // -----------------------------------------------
        // 5️⃣ View Verified Claims
        // -----------------------------------------------
        public IActionResult VerifiedClaims()
        {
            try
            {
                var verifiedClaims = _repo.GetAllClaims()
                    .Where(c => c.claim_status == "Verified")
                    .OrderByDescending(c => c.verified_date)
                    .ToList();

                return View(verifiedClaims);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading verified claims: {ex.Message}";
                return View(Enumerable.Empty<Claim>());
            }
        }
    }
}