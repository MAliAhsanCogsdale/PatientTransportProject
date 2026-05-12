using Microsoft.EntityFrameworkCore;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Repositories.Implementation
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(x => x.Username == username && x.IsActive && x.Deleted == null);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching user by username", ex);
            }
        }

        public async Task CreateAsync(User user)
        {
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating user", ex);
            }
        }

        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            try
            {
                return await _context.Roles.FirstOrDefaultAsync(x => x.Id == roleId && x.IsActive && x.Deleted == null);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching role by id", ex);
            }
        }
    }
}
