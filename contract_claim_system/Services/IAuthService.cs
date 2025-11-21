using contract_claim_system.Models;

namespace contract_claim_system.Services
{
    public interface IAuthService
    {
        User Authenticate(string email, string password);
        bool Register(User user);
        bool EmailExists(string email);
        User GetUserById(int userId);
        bool UpdateUser(User user);
        bool ChangePassword(int userId, string newPassword);
    }
}