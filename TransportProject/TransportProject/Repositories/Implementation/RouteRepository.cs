using Microsoft.EntityFrameworkCore;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using RouteModel = TransportProject.Models.Route;
using TransportProject.Repositories.Interface;

namespace TransportProject.Repositories.Implementation
{
    public class RouteRepository : IRouteRepository
    {
        private readonly ApplicationDbContext _context;
        public RouteRepository(ApplicationDbContext context) { _context = context; }

        public async Task<IEnumerable<RouteModel>> GetAllAsync()
        {
            try { return await _context.Routes.Where(x => x.IsActive && x.Deleted == null).ToListAsync(); }
            catch (Exception ex) { throw new Exception("Error fetching routes", ex); }
        }

        public async Task<RouteModel?> GetByIdAsync(int id)
        {
            try { return await _context.Routes.FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.Deleted == null); }
            catch (Exception ex) { throw new Exception("Error fetching route", ex); }
        }

        public async Task<IEnumerable<RouteModel>> GetByDriverIdAsync(int driverId)
        {
            try { return await _context.Routes.Where(x => x.DriverId == driverId && x.IsActive && x.Deleted == null).ToListAsync(); }
            catch (Exception ex) { throw new Exception("Error fetching routes for driver", ex); }
        }

        public async Task<IEnumerable<RouteModel>> GetByDateAsync(DateTime date)
        {
            try { return await _context.Routes.Where(x => x.RouteDate.Date == date.Date && x.IsActive && x.Deleted == null).ToListAsync(); }
            catch (Exception ex) { throw new Exception("Error fetching routes for date", ex); }
        }

        public async Task CreateAsync(RouteModel route)
        {
            try { route.IsActive = true; route.Deleted = null; await _context.Routes.AddAsync(route); await _context.SaveChangesAsync(); }
            catch (Exception ex) { throw new Exception("Error creating route", ex); }
        }

        public async Task UpdateAsync(RouteModel route)
        {
            try { _context.Routes.Update(route); await _context.SaveChangesAsync(); }
            catch (Exception ex) { throw new Exception("Error updating route", ex); }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var route = await _context.Routes.FindAsync(id);
                if (route != null) { route.IsActive = false; route.Deleted = DateTime.UtcNow; await _context.SaveChangesAsync(); }
            }
            catch (Exception ex) { throw new Exception("Error deleting route", ex); }
        }
    }
}
