using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Controllers
{
    [Authorize(Roles = "Admin,Dispatcher")]
    public class RouteController : Controller
    {
        private readonly IRouteRepository _repository;

        public RouteController(IRouteRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index(string search)
        {
            var items = await _repository.GetAllAsync();

            if (!string.IsNullOrEmpty(search))
            {
                items = items.Where(x => x.Status.Contains(search));
            }

            return View(items);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Models.Route route)
        {
            await _repository.CreateAsync(route);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Models.Route route)
        {
            await _repository.UpdateAsync(route);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
