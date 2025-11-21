using System.Collections.Generic;
using contract_claim_system.Models;

namespace contract_claim_system.Data
{
    public interface IClaimRepository
    {
        int CreateClaim(Claim claim);
        Claim GetClaimById(int id);
        IEnumerable<Claim> GetClaimsByLecturer(int lecturerID);
        IEnumerable<Claim> GetAllClaims();
        bool UpdateClaimStatus(int id, string status);
        bool UpdateClaim(Claim claim); // ADD THIS METHOD
    }
}