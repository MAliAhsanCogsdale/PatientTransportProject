using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace TransportProject.Repositories.Implementation
{
    public class DriverRepository : IDriverRepository
    {
        private readonly ApplicationDbContext _context;
        public DriverRepository(ApplicationDbContext context) { _context = context; }

        public async Task<IEnumerable<Driver>> GetAllAsync()
        {
            try { return await _context.Drivers.Where(x => x.IsActive && x.Deleted == null).ToListAsync(); }
            catch (Exception ex) { throw new Exception("Error fetching drivers", ex); }
        }

        public async Task<Driver?> GetByIdAsync(int id)
        {
            try { return await _context.Drivers.FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.Deleted == null); }
            catch (Exception ex) { throw new Exception("Error fetching driver", ex); }
        }

        public async Task AddAsync(Driver entity)
        {
            try { await _context.Drivers.AddAsync(entity); await _context.SaveChangesAsync(); }
            catch (Exception ex) { throw new Exception("Error adding driver", ex); }
        }

        public async Task UpdateAsync(Driver entity)
        {
            try { _context.Drivers.Update(entity); await _context.SaveChangesAsync(); }
            catch (Exception ex) { throw new Exception("Error updating driver", ex); }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var driver = await _context.Drivers.FindAsync(id);
                if (driver != null) { driver.IsActive = false; driver.Deleted = DateTime.UtcNow; await _context.SaveChangesAsync(); }
            }
            catch (Exception ex) { throw new Exception("Error deleting driver", ex); }
        }
    }
}
