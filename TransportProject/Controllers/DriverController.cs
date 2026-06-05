using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TransportProject.Helper_Classes;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Controllers
{
    [Authorize(Roles = "Admin,Dispatcher")]
    public class DriverController : Controller
    {
        private readonly IDriverRepository _repository;

        public DriverController(IDriverRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index(string search, int page = 1, int pageSize = 10)
        {
            var query = (await _repository.GetAllAsync()).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.FirstName.Contains(search) ||
                    x.LastName.Contains(search) ||
                    x.Phone.Contains(search) ||
                    x.VehicleId.ToString().Contains(search) ||
                    x.Status.Contains(search));
            }

            var result = query.ToPagedResult(page, pageSize, search);

            return View(result);
        }
        public async Task<IActionResult> Details(int id)
        {
            var driver = await _repository.GetByIdAsync(id);

            if (driver == null)
                return NotFound();

            return View(driver);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Driver driver)
        {
            await _repository.AddAsync(driver);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var driver = await _repository.GetByIdAsync(id);
            return View(driver);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Driver driver)
        {
            await _repository.UpdateAsync(driver);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}