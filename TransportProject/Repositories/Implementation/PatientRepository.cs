using Microsoft.EntityFrameworkCore;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Repositories.Implementation
{
    public class PatientRepository : IPatientRepository
    {
        private readonly ApplicationDbContext _context;

        public PatientRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Patient>> GetAllAsync()
        {
            try
            {
                return await _context.Patients
                    .Where(x => x.Deleted == null && x.IsActive)
                    .ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Patient?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Patients
                    .FirstOrDefaultAsync(x => x.Id == id && x.Deleted == null);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task CreateAsync(Patient patient)
        {
            try
            {
                patient.IsActive = true;
                patient.Deleted = null;

                await _context.Patients.AddAsync(patient);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UpdateAsync(Patient patient)
        {
            try
            {
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(id);

                if (patient != null)
                {
                    patient.Deleted = DateTime.Now;
                    patient.IsActive = false;

                    _context.Patients.Update(patient);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}