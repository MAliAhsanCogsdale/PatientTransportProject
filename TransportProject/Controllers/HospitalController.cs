using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TransportProject.Helper_Classes;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Controllers
{
    [Authorize(Roles = "Admin,Dispatcher")]
    public class HospitalController : Controller
    {
        private readonly IHospitalRepository _repository;

        public HospitalController(IHospitalRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index(string search, int page = 1, int pageSize = 10)
        {
            var data = await _repository.GetAllAsync() ?? new List<Hospital>();
            var query = data.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x =>
                    x.Name.Contains(search) ||
                    (x.Address ?? "").Contains(search) ||
                    (x.Phone ?? "").Contains(search));
            }

            var result = query.ToPagedResult(page, pageSize, search);

            return View(result);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Hospital hospital)
        {
            await _repository.CreateAsync(hospital);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Hospital hospital)
        {
            await _repository.UpdateAsync(hospital);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return NotFound();

            return View(item);
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
