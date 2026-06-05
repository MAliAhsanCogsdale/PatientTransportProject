using TransportProject.Models;
using TransportProject.ViewModels;

namespace TransportProject.Repositories.Interface
{
    public interface IRouteAppointmentRepository
    {
        IQueryable<RouteAppointmentVM> Query();
        Task<List<RouteAppointmentVM>> GetAllAsync();
        Task<RouteAppointment?> GetByIdAsync(int id);
        Task<List<RouteAppointmentVM>> GetByRouteIdAsync(int routeId);
        //Task CreateAsync(RouteAppointment routeAppointment);
        Task UpdateAsync(RouteAppointment routeAppointment);
        Task DeleteAsync(int id);
    }
}
