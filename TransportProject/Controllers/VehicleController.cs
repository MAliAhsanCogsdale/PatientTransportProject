using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Controllers
{
    [Authorize(Roles = "Admin,Dispatcher")]
    public class VehicleController : Controller
    {
        private readonly IVehicleRepository _repository;

        public VehicleController(IVehicleRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index(string search)
        {
            var items = (await _repository.GetAllAsync())
                        .Where(x => x.Deleted == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                items = items.Where(x =>
                    x.VehicleNumber.Contains(search) ||
                    x.VehicleName.Contains(search));
            }

            return View(items.ToList());
        }
        public async Task<IActionResult> Details(int id)
        {
            var item = await _repository.GetByIdAsync(id);

            if (item == null || item.Deleted != null)
                return NotFound();

            return View(item);
        }
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Vehicle vehicle)
        {
            if (!ModelState.IsValid)
                return View(vehicle);

            await _repository.CreateAsync(vehicle);
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _repository.GetByIdAsync(id);

            if (item == null || item.Deleted != null)
                return NotFound();

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Vehicle vehicle)
        {
            if (!ModelState.IsValid)
                return View(vehicle);

            await _repository.UpdateAsync(vehicle);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}