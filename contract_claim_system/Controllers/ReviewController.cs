using contract_claim_system.Data;
using contract_claim_system.Models;
using contract_claim_system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace contract_claim_system.Controllers
{
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public class ReviewController : Controller
    {
        private readonly IApprovalWorkflowService _workflowService;
        private readonly IClaimRepository _claimRepo;

        public ReviewController(IApprovalWorkflowService workflowService, IClaimRepository claimRepo)
        {
            _workflowService = workflowService;
            _claimRepo = claimRepo;
        }

        // Dashboard Actions
        public IActionResult CoordinatorDashboard()
        {
            var pendingClaims = _workflowService.GetClaimsForReview("Coordinator");
            return View(pendingClaims);
        }

        public IActionResult ManagerDashboard()
        {
            var approvedClaims = _workflowService.GetClaimsForReview("Manager");
            return View(approvedClaims);
        }

        // Claim Review Actions
        public IActionResult ReviewClaim(int id)
        {
            var claim = _claimRepo.GetClaimById(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found";
                return RedirectToAction(nameof(CoordinatorDashboard));
            }

            var validationResult = _workflowService.ValidateClaim(claim);
            ViewBag.ValidationResult = validationResult;

            return View(claim);
        }

        [HttpPost]
        public IActionResult ApproveAsCoordinator(int claimId)
        {
            var approvedBy = GetCurrentUserName();
            var success = _workflowService.ProcessApproval(claimId, approvedBy, "Coordinator");

            SetResultMessage(success,
                "Claim approved successfully and sent to Manager for final approval",
                "Failed to approve claim");

            return RedirectToAction(nameof(CoordinatorDashboard));
        }

        [HttpPost]
        public IActionResult FinalApprove(int claimId)
        {
            var approvedBy = GetCurrentUserName();
            var success = _workflowService.ProcessApproval(claimId, approvedBy, "Manager");

            if (success)
            {
                _claimRepo.UpdateClaimStatus(claimId, "Approved");
                TempData["Success"] = "Claim finally approved and sent to HR for processing";
            }
            else
            {
                TempData["Error"] = "Failed to approve claim";
            }

            return RedirectToAction(nameof(ManagerDashboard));
        }

        [HttpPost]
        public IActionResult RejectClaim(int claimId, string rejectionReason)
        {
            try
            {
                var rejectedBy = GetCurrentUserName();
                _claimRepo.UpdateClaimStatus(claimId, "Rejected");

                // TODO: Log rejection with reason in database
                LogRejection(claimId, rejectedBy, rejectionReason);

                TempData["Success"] = "Claim rejected successfully";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error rejecting claim: {ex.Message}";
            }

            return RedirectToAction(nameof(CoordinatorDashboard));
        }

        // Private Helper Methods
        private string GetCurrentUserName()
        {
            return User.Identity?.Name ?? "System User";
        }

        private void SetResultMessage(bool success, string successMessage, string errorMessage)
        {
            if (success)
                TempData["Success"] = successMessage;
            else
                TempData["Error"] = errorMessage;
        }

        private void LogRejection(int claimId, string rejectedBy, string reason)
        {
            // Implementation for logging rejections would go here
            // This could be added to the ApprovalLogs table or a separate RejectionLogs table
            // For now, this is a placeholder for future implementation
        }
    }
}