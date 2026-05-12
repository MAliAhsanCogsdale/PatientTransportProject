using Microsoft.EntityFrameworkCore;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Repositories.Implementation
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly ApplicationDbContext _context;
        public AppointmentRepository(ApplicationDbContext context) { _context = context; }

        public async Task<IEnumerable<Appointment>> GetAllAsync()
        {
            try { return await _context.Appointments.Where(x => x.IsActive && x.Deleted == null).ToListAsync(); }
            catch (Exception ex) { throw new Exception("Error fetching appointments", ex); }
        }

        public async Task<Appointment?> GetByIdAsync(int id)
        {
            try { return await _context.Appointments.FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.Deleted == null); }
            catch (Exception ex) { throw new Exception("Error fetching appointment", ex); }
        }

        public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(int patientId)
        {
            try { return await _context.Appointments.Where(x => x.PatientId == patientId && x.IsActive && x.Deleted == null).ToListAsync(); }
            catch (Exception ex) { throw new Exception("Error fetching appointments for patient", ex); }
        }

        public async Task CreateAsync(Appointment appointment)
        {
            try
            {
                appointment.IsActive = true;
                appointment.Deleted = null;

                await _context.Appointments.AddAsync(appointment);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) { throw new Exception("Error creating appointment", ex); }
        }

        public async Task UpdateAsync(Appointment appointment)
        {
            try { _context.Appointments.Update(appointment); await _context.SaveChangesAsync(); }
            catch (Exception ex) { throw new Exception("Error updating appointment", ex); }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment != null) { appointment.IsActive = false; appointment.Deleted = DateTime.UtcNow; await _context.SaveChangesAsync(); }
            }
            catch (Exception ex) { throw new Exception("Error deleting appointment", ex); }
        }
    }
}
