using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TransportProject.Helper_Classes;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

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

        public async Task<IActionResult> Index(string search, int page = 1, int pageSize = 10)
        {
            var query = (await _repository.GetAllAsync()).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x =>
                    x.FirstName.Contains(search) ||
                    x.LastName.Contains(search) ||
                    x.PhoneNumber.Contains(search));
            }

            var result = query.ToPagedResult(page, pageSize, search);

            return View(result);
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
        public async Task<IActionResult> Details(int id)
        {
            var patient = await _repository.GetByIdAsync(id);

            if (patient == null)
                return NotFound();

            return View(patient);
        }
    }
}