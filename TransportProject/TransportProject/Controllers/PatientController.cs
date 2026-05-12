using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportProject.Models;
using TransportProject.Repositories.Interface;
using System.Linq;

namespace TransportProject.Controllers
{
    [Authorize]
    public class PatientController : Controller
    {
        private readonly IPatientRepository _repository;

        public PatientController(IPatientRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index(string search)
        {
            try
            {
                var patients = await _repository.GetAllAsync();

                if (!string.IsNullOrEmpty(search))
                {
                    patients = patients.Where(x =>
                        x.FirstName.Contains(search) ||
                        x.LastName.Contains(search) ||
                        x.PhoneNumber.Contains(search));
                }

                return View(patients);
            }
            catch
            {
                return View(new List<Patient>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Patient patient)
        {
            try
            {
                await _repository.CreateAsync(patient);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(patient);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var patient = await _repository.GetByIdAsync(id);
            return View(patient);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Patient patient)
        {
            await _repository.UpdateAsync(patient);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}