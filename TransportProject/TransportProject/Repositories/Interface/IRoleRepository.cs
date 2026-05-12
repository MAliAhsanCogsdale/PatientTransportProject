using TransportProject.Models;

namespace TransportProject.Repositories.Interface
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(int id);
        Task<Role?> GetByNameAsync(string name);
    }
}
