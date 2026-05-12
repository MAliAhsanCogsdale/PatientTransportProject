using Microsoft.EntityFrameworkCore;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Repositories.Implementation
{
    public class HospitalRepository : IHospitalRepository
    {
        private readonly ApplicationDbContext _context;
        public HospitalRepository(ApplicationDbContext context) { _context = context; }

        public async Task<IEnumerable<Hospital>> GetAllAsync()
        {
            try { return await _context.Hospitals.Where(x => x.IsActive && x.Deleted == null).ToListAsync(); }
            catch (Exception ex) { throw new Exception("Error fetching hospitals", ex); }
        }

        public async Task<Hospital?> GetByIdAsync(int id)
        {
            try { return await _context.Hospitals.FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.Deleted == null); }
            catch (Exception ex) { throw new Exception("Error fetching hospital", ex); }
        }

        public async Task CreateAsync(Hospital hospital)
        {
            try
            {
                hospital.IsActive = true; hospital.Deleted = null; await _context.Hospitals.AddAsync(hospital); await _context.SaveChangesAsync();
            }
            catch (Exception ex) { throw new Exception("Error creating hospital", ex); }
        }

        public async Task UpdateAsync(Hospital hospital)
        {
            try { _context.Hospitals.Update(hospital); await _context.SaveChangesAsync(); }
            catch (Exception ex) { throw new Exception("Error updating hospital", ex); }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var hospital = await _context.Hospitals.FindAsync(id);
                if (hospital != null) { hospital.IsActive = false; hospital.Deleted = DateTime.UtcNow; await _context.SaveChangesAsync(); }
            }
            catch (Exception ex) { throw new Exception("Error deleting hospital", ex); }
        }
    }
}
