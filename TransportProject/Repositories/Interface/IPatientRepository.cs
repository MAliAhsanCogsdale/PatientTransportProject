using TransportProject.Models;

namespace TransportProject.Repositories.Interface
{
    public interface IPatientRepository
    {
        Task<IEnumerable<Patient>> GetAllAsync();

        Task<Patient?> GetByIdAsync(int id);

        Task CreateAsync(Patient patient);

        Task UpdateAsync(Patient patient);

        Task DeleteAsync(int id);
    }
}
