using TransportProject.Models;

namespace TransportProject.Repositories.Interface
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(int id);
        Task<Role?> GetByNameAsync(string name);
        Task CreateAsync(Role role);
        Task UpdateAsync(Role role);
        Task DeleteAsync(int id);
        Task<List<Role>> GetAllAsync();
    }
}
