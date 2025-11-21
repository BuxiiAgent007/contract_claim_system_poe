using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using contract_claim_system.Models;

namespace contract_claim_system.Data
{
    public class ClaimRepository : IClaimRepository
    {
        private readonly string _connectionString;

        public ClaimRepository(string connectionString)
        {
            _connectionString = connectionString;
            Console.WriteLine($"ClaimRepository using: {connectionString}");
        }

        public int CreateClaim(Claim claim)
        {
            const string query = @"
INSERT INTO Claims (number_of_sessions, number_of_hours, amount_of_rate, module_name, faculty_name, supporting_documents, claim_status, creating_date, lecturerID)
VALUES (@sessions, @hours, @rate, @module, @faculty, @doc, @status, @date, @lecturerID);
SELECT SCOPE_IDENTITY();";

            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, con))
                {
                    Console.WriteLine($"Connection String: {_connectionString}");

                    cmd.Parameters.AddWithValue("@sessions", claim.number_of_sessions);
                    cmd.Parameters.AddWithValue("@hours", claim.number_of_hours);
                    cmd.Parameters.AddWithValue("@rate", claim.amount_of_rate);
                    cmd.Parameters.AddWithValue("@module", claim.module_name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@faculty", claim.faculty_name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@doc", claim.supporting_documents ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", claim.claim_status ?? "Pending");
                    cmd.Parameters.AddWithValue("@date", claim.creating_date == default ? DateTime.Now : claim.creating_date);
                    cmd.Parameters.AddWithValue("@lecturerID", claim.lecturerID);

                    con.Open();
                    Console.WriteLine("Database connection opened successfully");

                    var result = cmd.ExecuteScalar();
                    Console.WriteLine($"Claim inserted successfully, ID: {result}");

                    return Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CreateClaim: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public Claim GetClaimById(int id)
        {
            const string query = "SELECT * FROM Claims WHERE claimID = @id";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapReader(reader);
                }
            }

            return null;
        }

        public IEnumerable<Claim> GetClaimsByLecturer(int lecturerID)
        {
            var list = new List<Claim>();
            const string query = "SELECT * FROM Claims WHERE lecturerID = @lecturerID";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@lecturerID", lecturerID);
                con.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapReader(reader));
                }
            }

            return list;
        }

        public IEnumerable<Claim> GetAllClaims()
        {
            var list = new List<Claim>();
            const string query = "SELECT * FROM Claims ORDER BY creating_date DESC";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, con))
            {
                con.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(MapReader(reader));
                }
            }

            return list;
        }

        public bool UpdateClaimStatus(int id, string status)
        {
            const string query = "UPDATE Claims SET claim_status = @status WHERE claimID = @id";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@status", status);

                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool UpdateClaim(Claim claim)
        {
            const string query = @"
UPDATE Claims 
SET number_of_sessions = @sessions,
    number_of_hours = @hours,
    amount_of_rate = @rate,
    module_name = @module,
    faculty_name = @faculty,
    supporting_documents = @doc,
    claim_status = @status,
    creating_date = @date,
    lecturerID = @lecturerID,
    verified_by = @verifiedBy,
    verified_date = @verifiedDate,
    approved_by = @approvedBy,
    approved_date = @approvedDate,
    rejection_reason = @rejectionReason
WHERE claimID = @id";

            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", claim.claimID);
                    cmd.Parameters.AddWithValue("@sessions", claim.number_of_sessions);
                    cmd.Parameters.AddWithValue("@hours", claim.number_of_hours);
                    cmd.Parameters.AddWithValue("@rate", claim.amount_of_rate);
                    cmd.Parameters.AddWithValue("@module", claim.module_name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@faculty", claim.faculty_name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@doc", claim.supporting_documents ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", claim.claim_status ?? "Pending");
                    cmd.Parameters.AddWithValue("@date", claim.creating_date);
                    cmd.Parameters.AddWithValue("@lecturerID", claim.lecturerID);

                    // Handle nullable fields for workflow
                    cmd.Parameters.AddWithValue("@verifiedBy", claim.verified_by ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@verifiedDate", claim.verified_date.HasValue ? (object)claim.verified_date.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@approvedBy", claim.approved_by ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@approvedDate", claim.approved_date.HasValue ? (object)claim.approved_date.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@rejectionReason", claim.rejection_reason ?? (object)DBNull.Value);

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    Console.WriteLine($"UpdateClaim: {rowsAffected} row(s) affected for Claim ID: {claim.claimID}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateClaim: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private Claim MapReader(SqlDataReader r)
        {
            return new Claim
            {
                claimID = Convert.ToInt32(r["claimID"]),
                number_of_sessions = Convert.ToInt32(r["number_of_sessions"]),
                number_of_hours = Convert.ToInt32(r["number_of_hours"]),
                amount_of_rate = Convert.ToInt32(r["amount_of_rate"]),
                module_name = r["module_name"]?.ToString(),
                faculty_name = r["faculty_name"]?.ToString(),
                supporting_documents = r["supporting_documents"]?.ToString(),
                claim_status = r["claim_status"]?.ToString(),
                creating_date = Convert.ToDateTime(r["creating_date"]),
                lecturerID = Convert.ToInt32(r["lecturerID"]),

                // Map the new workflow fields (handle DBNull)
                verified_by = r["verified_by"] as string,
                verified_date = r["verified_date"] as DateTime?,
                approved_by = r["approved_by"] as string,
                approved_date = r["approved_date"] as DateTime?,
                rejection_reason = r["rejection_reason"] as string
            };
        }
    }
}