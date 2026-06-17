using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Repositories.Implementation
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        public AuthRepository(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive && u.Deleted == null);

            if (user == null)
                return null;

            // Enforce lockout
            if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
                return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);

            if (result == PasswordVerificationResult.Failed)
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= MaxFailedAttempts)
                {
                    user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                    user.FailedLoginCount = 0;
                }
                await _context.SaveChangesAsync();
                return null;
            }

            // Success – reset counters
            user.FailedLoginCount = 0;
            user.LockoutEndUtc = null;
            user.LastLoginUtc = DateTime.UtcNow;

            // Transparently upgrade legacy/weak hashes
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
                user.Password = _passwordHasher.HashPassword(user, password);

            await _context.SaveChangesAsync();
            return user;

            //if (user.Password == password)
            //    return user;

            //return null;
        }

        public async Task RegisterAsync(User user)
        {
            try
            {
                // Hash the incoming plaintext password before saving
                user.Password = _passwordHasher.HashPassword(user, user.Password);
                user.IsActive = true;
                user.Deleted = null;
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error registering user", ex);
            }

            //try { user.IsActive = true; user.Deleted = null; await _context.Users.AddAsync(user); await _context.SaveChangesAsync(); }
            //catch (Exception ex) { throw new Exception("Error registering user", ex); }

        }
    }
}
