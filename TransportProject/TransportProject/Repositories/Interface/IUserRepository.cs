using TransportProject.Models;

namespace TransportProject.Repositories.Interface
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task CreateAsync(User user);
        Task<Role?> GetRoleByIdAsync(int roleId);
    }
}
