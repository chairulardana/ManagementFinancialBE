using AuthAPI.Models;

namespace AuthAPI.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> RegisterUserAsync(User user);
    }
}
