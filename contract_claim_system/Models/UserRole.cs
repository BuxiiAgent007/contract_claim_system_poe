using System.ComponentModel.DataAnnotations;
public class UserRole
{
    public int UserId { get; set; }
    public string Role { get; set; } // Lecturer, Coordinator, Manager, HR
    public string Department { get; set; }
}

// Models/ApprovalWorkflow.cs
public class ApprovalWorkflow
{
    public int WorkflowId { get; set; }
    public int ClaimId { get; set; }
    public string CurrentStage { get; set; } // Submitted → Verified → Approved → Processed
    public string ActionBy { get; set; }
    public DateTime ActionDate { get; set; }
    public string Comments { get; set; }
}