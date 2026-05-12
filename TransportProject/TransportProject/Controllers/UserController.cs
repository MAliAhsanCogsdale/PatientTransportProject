using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IUserRepository _repository;

        public UserController(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index(string search)
        {
            // user repository does not expose GetAll; show placeholder
            return View(new List<User>());
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
            await _repository.CreateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string username)
        {
            var user = await _repository.GetByUsernameAsync(username);
            return View(user);
        }
    }
}
