using Microsoft.EntityFrameworkCore;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;
using TransportProject.ViewModels;

namespace TransportProject.Repositories.Implementation
{
    public class RouteAppointmentRepository : IRouteAppointmentRepository
    {
        private readonly ApplicationDbContext _context;
        public IQueryable<RouteAppointmentVM> Query()
        {
            return _context.RouteAppointments
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.Id)
                .Select(x => new RouteAppointmentVM
                {
                    Id = x.Id,
                    DriverName = x.Route.Driver.FirstName + " " + x.Route.Driver.LastName,
                    PatientName = x.Appointment.Patient.FirstName + " " + x.Appointment.Patient.LastName,
                    PickupTime = x.Appointment.PickupTime,
                    PickupAddress = x.Appointment.PickupAddress,
                    HospitalName = x.Appointment.Hospital.Name,
                    SequenceOrder = x.SequenceOrder
                });
        }
        public RouteAppointmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<RouteAppointmentVM>> GetAllAsync()
        {
            try
            {
                return await (
                    from ra in _context.RouteAppointments
                    join r in _context.Routes on ra.RouteId equals r.Id
                    join d in _context.Drivers on r.DriverId equals d.Id
                    join a in _context.Appointments on ra.AppointmentId equals a.Id
                    join p in _context.Patients on a.PatientId equals p.Id
                    join h in _context.Hospitals on a.HospitalId equals h.Id
                    where ra.IsActive && ra.Deleted == null
                    orderby ra.SequenceOrder
                    select new RouteAppointmentVM
                    {
                        Id = ra.Id,
                        DriverName = d.FirstName + " " + d.LastName,
                        PatientName = p.FirstName + " " + p.LastName,
                        PickupTime = a.PickupTime,
                        PickupAddress = a.PickupAddress,
                        HospitalName = h.Name,
                        SequenceOrder = ra.SequenceOrder
                    }
                ).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching route appointments", ex);
            }
        }
        public async Task<RouteAppointment?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Set<RouteAppointment>()
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.Deleted == null);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching route appointment by ID", ex);
            }
        }
        public async Task<List<RouteAppointmentVM>> GetByRouteIdAsync(int routeId)
        {
            try
            {
                return await (
                    from ra in _context.RouteAppointments
                    join r in _context.Routes on ra.RouteId equals r.Id
                    join d in _context.Drivers on r.DriverId equals d.Id
                    join a in _context.Appointments on ra.AppointmentId equals a.Id
                    join p in _context.Patients on a.PatientId equals p.Id
                    join h in _context.Hospitals on a.HospitalId equals h.Id
                    where ra.RouteId == routeId && ra.IsActive && ra.Deleted == null
                    orderby ra.SequenceOrder
                    select new RouteAppointmentVM
                    {
                        Id = ra.Id,
                        DriverName = d.FirstName + " " + d.LastName,
                        PatientName = p.FirstName + " " + p.LastName,
                        PickupTime = a.PickupTime,
                        PickupAddress = a.PickupAddress,
                        HospitalName = h.Name,
                        SequenceOrder = ra.SequenceOrder
                    }
                ).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching route appointments for route ID {routeId}", ex);
            }
        }
        //public async Task CreateAsync(RouteAppointment routeAppointment)
        //{
        //    try
        //    {
        //        routeAppointment.IsActive = true;
        //        routeAppointment.Deleted = null;

        //        await _context.Set<RouteAppointment>().AddAsync(routeAppointment);
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error creating route appointment", ex);
        //    }
        //}
        public async Task UpdateAsync(RouteAppointment routeAppointment)
        {
            try
            {
                _context.Set<RouteAppointment>().Update(routeAppointment);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating route appointment", ex);
            }
        }
        public async Task DeleteAsync(int id)
        {
            try
            {
                var ra = await _context.Set<RouteAppointment>().FindAsync(id);
                if (ra != null)
                {
                    ra.IsActive = false;
                    ra.Deleted = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting route appointment", ex);
            }
        }
    }
}