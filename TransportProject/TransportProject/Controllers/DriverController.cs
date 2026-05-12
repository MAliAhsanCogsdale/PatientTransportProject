using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportProject.Models;
using TransportProject.Repositories.Interface;
using System.Threading.Tasks;

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

        public async Task<IActionResult> Index()
        {
            var drivers = await _repository.GetAllAsync();
            return View(drivers);
        }

        public IActionResult Create()
        {
            return View();
        }

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