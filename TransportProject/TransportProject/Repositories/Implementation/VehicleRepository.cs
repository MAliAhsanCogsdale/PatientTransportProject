using Microsoft.EntityFrameworkCore;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Repositories.Implementation
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly ApplicationDbContext _context;
        public VehicleRepository(ApplicationDbContext context) { _context = context; }

        public async Task<IEnumerable<Vehicle>> GetAllAsync()
        {
            try { return await _context.Vehicles.Where(x => x.IsActive && x.Deleted == null).ToListAsync(); }
            catch (Exception ex) { throw new Exception("Error fetching vehicles", ex); }
        }

        public async Task<Vehicle?> GetByIdAsync(int id)
        {
            try { return await _context.Vehicles.FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.Deleted == null); }
            catch (Exception ex) { throw new Exception("Error fetching vehicle", ex); }
        }

        public async Task CreateAsync(Vehicle vehicle)
        {
            try { vehicle.IsActive = true; vehicle.Deleted = null; await _context.Vehicles.AddAsync(vehicle); await _context.SaveChangesAsync(); }
            catch (Exception ex) { throw new Exception("Error creating vehicle", ex); }
        }

        public async Task UpdateAsync(Vehicle vehicle)
        {
            try { _context.Vehicles.Update(vehicle); await _context.SaveChangesAsync(); }
            catch (Exception ex) { throw new Exception("Error updating vehicle", ex); }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle != null) { vehicle.IsActive = false; vehicle.Deleted = DateTime.UtcNow; await _context.SaveChangesAsync(); }
            }
            catch (Exception ex) { throw new Exception("Error deleting vehicle", ex); }
        }
    }
}
