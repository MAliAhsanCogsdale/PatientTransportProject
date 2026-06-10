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
            // use explicit joins so EF can translate cleanly and avoid navigation nullability surprises
            var q = from ra in _context.Appointments
                    join d in _context.Drivers on ra.DriverId equals d.Id into dj
                    from d in dj.DefaultIfEmpty()
                    join a in _context.Appointments on ra.Id equals a.Id into aj
                    from a in aj.DefaultIfEmpty()
                    join p in _context.Patients on a.PatientId equals p.Id into pj
                    from p in pj.DefaultIfEmpty()
                    join h in _context.Hospitals on a.HospitalId equals h.Id into hj
                    from h in hj.DefaultIfEmpty()
                    where ra.IsActive && ra.Deleted == null
                    select new RouteAppointmentVM
                    {
                        Id = ra.Id,
                        DriverName = d != null ? (d.FirstName + " " + d.LastName) : string.Empty,
                        PatientName = p != null ? (p.FirstName + " " + p.LastName) : string.Empty,
                        PickupTime = a != null ? a.PickupTime : DateTime.MinValue,
                        PickupAddress = a != null ? a.PickupAddress : string.Empty,
                        HospitalName = h != null ? h.Name : string.Empty,
                        SequenceOrder = ra.SequenceOrder,
                        LOS = ra.LOS,
                        CPay = ra.CPay ?? 0,
                        //PCA = ra.PCA,
                        //AESC = ra.AESC,
                        //CESC = ra.CESC,
                        //Seats = ra.Seats,
                        Miles = ra.Miles ?? 0,
                        Notes = ra.Notes
                    };

            return q;
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
                    from ra in _context.Appointments
                    join d in _context.Drivers on ra.DriverId equals d.Id
                    join p in _context.Patients on ra.PatientId equals p.Id
                    join h in _context.Hospitals on ra.HospitalId equals h.Id
                    where ra.IsActive && ra.Deleted == null
                    orderby ra.SequenceOrder
                    select new RouteAppointmentVM
                    {
                        Id = ra.Id,
                        DriverName = d.FirstName + " " + d.LastName,
                        PatientName = p.FirstName + " " + p.LastName,
                        PickupTime = ra.PickupTime,
                        PickupAddress = ra.PickupAddress,
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
        public async Task<Appointment?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Set<Appointment>()
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
                    from ra in _context.Appointments
                    join d in _context.Drivers on ra.DriverId equals d.Id
                    join p in _context.Patients on ra.PatientId equals p.Id
                    join h in _context.Hospitals on ra.HospitalId equals h.Id
                    where ra.Id == routeId && ra.IsActive && ra.Deleted == null
                    orderby ra.SequenceOrder
                    select new RouteAppointmentVM
                    {
                        Id = ra.Id,
                        DriverName = d.FirstName + " " + d.LastName,
                        PatientName = p.FirstName + " " + p.LastName,
                        PickupTime = ra.PickupTime,
                        PickupAddress = ra.PickupAddress,
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
        public async Task UpdateAsync(Appointment routeAppointment)
        {
            try
            {
                _context.Set<Appointment>().Update(routeAppointment);
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
                var ra = await _context.Set<Appointment>().FindAsync(id);
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