using TransportProject.Models;

namespace TransportProject.Repositories.Interface
{
    public interface IDriverRepository
    {
        Task<IEnumerable<Driver>> GetAllAsync();
        Task<Driver?> GetByIdAsync(int id);
        Task AddAsync(Driver entity);
        Task UpdateAsync(Driver entity);
        Task DeleteAsync(int id);
    }
}
