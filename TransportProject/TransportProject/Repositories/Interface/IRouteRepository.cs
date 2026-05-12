using TransportProject.Models;
using RouteModel = TransportProject.Models.Route;

namespace TransportProject.Repositories.Interface
{
    public interface IRouteRepository
    {
        Task<IEnumerable<RouteModel>> GetAllAsync();
        Task<RouteModel?> GetByIdAsync(int id);
        Task<IEnumerable<RouteModel>> GetByDriverIdAsync(int driverId);
        Task<IEnumerable<RouteModel>> GetByDateAsync(DateTime date);
        Task CreateAsync(RouteModel route);
        Task UpdateAsync(RouteModel route);
        Task DeleteAsync(int id);
    }
}
