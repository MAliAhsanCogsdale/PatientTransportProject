using TransportProject.Models;

namespace TransportProject.Repositories.Interface
{
    public interface IAuthRepository
    {
        Task<User?> LoginAsync(string username, string password);
        Task RegisterAsync(User user);
    }
}
