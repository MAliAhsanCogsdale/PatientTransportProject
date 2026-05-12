using Microsoft.EntityFrameworkCore;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Repositories.Implementation
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _context;
        public AuthRepository(ApplicationDbContext context) { _context = context; }

        public async Task<User?> LoginAsync(string username, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive && u.Deleted == null);

            if (user == null)
                return null;

            if (user.Password == password)
                return user;

            return null;
        }

        public async Task RegisterAsync(User user)
        {
            try { user.IsActive = true; user.Deleted = null; await _context.Users.AddAsync(user); await _context.SaveChangesAsync(); }
            catch (Exception ex) { throw new Exception("Error registering user", ex); }
        }
    }
}
