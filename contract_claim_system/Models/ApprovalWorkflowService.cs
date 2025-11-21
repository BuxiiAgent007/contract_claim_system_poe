using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using contract_claim_system.Models;
using Microsoft.Extensions.Configuration;

namespace contract_claim_system.Services
{
    public interface IApprovalWorkflowService
    {
        ApprovalResult ValidateClaim(Claim claim);
        bool ProcessApproval(int claimId, string approvedBy, string role);
        IEnumerable<Claim> GetClaimsForReview(string reviewerRole);
    }

    public class ApprovalWorkflowService : IApprovalWorkflowService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public ApprovalWorkflowService(IConfiguration config)
        {
            _config = config;
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public ApprovalResult ValidateClaim(Claim claim)
        {
            var result = new ApprovalResult { IsValid = true, ValidationMessages = new List<string>() };

            // Policy 1: Maximum hours per session
            if (claim.number_of_hours > 8)
            {
                result.ValidationMessages.Add("Warning: Hours exceed typical 8-hour session limit");
            }

            // Policy 2: Rate validation based on faculty
            var facultyRates = new Dictionary<string, (decimal min, decimal max)>
            {
                { "Science", (100, 500) },
                { "Engineering", (120, 600) },
                { "Business", (150, 700) },
                { "Arts", (80, 400) },
                { "Health Sciences", (130, 650) }
            };

            if (facultyRates.ContainsKey(claim.faculty_name))
            {
                var (min, max) = facultyRates[claim.faculty_name];
                if (claim.amount_of_rate < min)
                {
                    result.ValidationMessages.Add($"Rate below faculty minimum (R{min})");
                    result.IsValid = false;
                }
                if (claim.amount_of_rate > max)
                {
                    result.ValidationMessages.Add($"Rate above faculty maximum (R{max})");
                    result.IsValid = false;
                }
            }

            // Policy 3: Maximum monthly hours (simplified)
            if (claim.number_of_hours > 160)
            {
                result.ValidationMessages.Add("Hours exceed monthly maximum of 160");
                result.IsValid = false;
            }

            // Policy 4: Session validation
            if (claim.number_of_sessions < 1 || claim.number_of_sessions > 20)
            {
                result.ValidationMessages.Add("Invalid number of sessions (1-20 allowed)");
                result.IsValid = false;
            }

            return result;
        }

        public bool ProcessApproval(int claimId, string approvedBy, string role)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();

                // Update claim status based on role
                string newStatus = role == "Coordinator" ? "Coordinator Approved" : "Manager Approved";
                var cmd = new SqlCommand(
                    "UPDATE Claims SET claim_status = @status WHERE claimID = @id",
                    con);
                cmd.Parameters.AddWithValue("@status", newStatus);
                cmd.Parameters.AddWithValue("@id", claimId);

                // Log approval
                var logCmd = new SqlCommand(
                    @"INSERT INTO ApprovalLogs (claimID, approvedBy, role, action, timestamp) 
                      VALUES (@claimId, @approvedBy, @role, @action, GETDATE())",
                    con);
                logCmd.Parameters.AddWithValue("@claimId", claimId);
                logCmd.Parameters.AddWithValue("@approvedBy", approvedBy);
                logCmd.Parameters.AddWithValue("@role", role);
                logCmd.Parameters.AddWithValue("@action", $"Approved by {role}");

                return cmd.ExecuteNonQuery() > 0 && logCmd.ExecuteNonQuery() > 0;
            }
        }

        public IEnumerable<Claim> GetClaimsForReview(string reviewerRole)
        {
            var claims = new List<Claim>();
            string query = reviewerRole == "Coordinator"
                ? "SELECT * FROM Claims WHERE claim_status IN ('Pending', 'Coordinator Approved') ORDER BY creating_date DESC"
                : "SELECT * FROM Claims WHERE claim_status = 'Coordinator Approved' ORDER BY creating_date DESC";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, con))
            {
                con.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        claims.Add(new Claim
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
                        });
                    }
                }
            }
            return claims;
        }
    }

    public class ApprovalResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationMessages { get; set; }
        public string FinalStatus { get; set; }
    }
}