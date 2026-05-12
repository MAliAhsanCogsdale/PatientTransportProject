using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TransportProject.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Dashboard()
        {
        //    var patientsCount = await _patientRepository.GetAllAsync();
        //    var driversCount = await _driverRepository.GetAllAsync();
        //    var appointmentsCount = await _appointmentRepository.GetAllAsync();

        //    ViewBag.PatientsCount = patientsCount.Count();
        //    ViewBag.DriversCount = driversCount.Count();
        //    ViewBag.AppointmentsCount = appointmentsCount.Count();

            return View();
        }
    }
}
