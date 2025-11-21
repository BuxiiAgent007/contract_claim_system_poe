using System.ComponentModel.DataAnnotations;

namespace contract_claim_system.Models
{
    public class Claim
    {
        public int claimID { get; set; }

        [Required(ErrorMessage = "Number of sessions is required")]
        [Range(1, 100, ErrorMessage = "Sessions must be between 1 and 100")]
        public int number_of_sessions { get; set; }

        [Required(ErrorMessage = "Number of hours is required")]
        [Range(1, 1000, ErrorMessage = "Hours must be between 1 and 1000")]
        public int number_of_hours { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(1, 1000, ErrorMessage = "Rate must be between 1 and 1000")]
        public int amount_of_rate { get; set; }

        [Required(ErrorMessage = "Module name is required")]
        [StringLength(100, ErrorMessage = "Module name cannot exceed 100 characters")]
        public string module_name { get; set; }

        [Required(ErrorMessage = "Faculty name is required")]
        [StringLength(100, ErrorMessage = "Faculty name cannot exceed 100 characters")]
        public string faculty_name { get; set; }

        public string supporting_documents { get; set; }

        public string claim_status { get; set; } = "Pending";

        public DateTime creating_date { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Lecturer ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Valid Lecturer ID is required")]
        public int lecturerID { get; set; }

        // Workflow properties (nullable)
        public string verified_by { get; set; }
        public DateTime? verified_date { get; set; }
        public string approved_by { get; set; }
        public DateTime? approved_date { get; set; }
        public string rejection_reason { get; set; }

        // Calculated property
        public decimal TotalAmount => number_of_hours * amount_of_rate;

        // Status helper properties
        public bool IsPending => claim_status == "Pending";
        public bool IsVerified => claim_status == "Verified";
        public bool IsApproved => claim_status == "Approved";
        public bool IsRejected => claim_status == "Rejected";
        public bool IsQueried => claim_status == "Query";
    }
}