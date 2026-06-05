using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly IRoleRepository _repository;

        public RoleController(IRoleRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index(string search)
        {
            var roles = await _repository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                roles = roles
                    .Where(x => x.Name.Contains(search))
                    .ToList();
            }

            return View(roles);
        }

        public async Task<IActionResult> Details(int id)
        {
            var role = await _repository.GetByIdAsync(id);
            if (role == null)
                return NotFound();

            return View(role);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Role role)
        {
            if (!ModelState.IsValid)
                return View(role);

            await _repository.CreateAsync(role);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var role = await _repository.GetByIdAsync(id);

            if (role == null)
                return NotFound();

            return View(role);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Role role)
        {
            if (!ModelState.IsValid)
                return View(role);

            await _repository.UpdateAsync(role);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _repository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}