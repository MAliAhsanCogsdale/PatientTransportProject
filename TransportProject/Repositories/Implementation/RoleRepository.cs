using Microsoft.EntityFrameworkCore;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Repositories.Implementation
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public RoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Role>> GetAllAsync()
        {
            return await _context.Roles
                .Where(x => x.Deleted == null)
                .ToListAsync();
        }

        public async Task<Role?> GetByIdAsync(int id)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(x => x.Id == id && x.Deleted == null);
        }

        public async Task<Role?> GetByNameAsync(string name)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(x => x.Name == name && x.Deleted == null);
        }

        public async Task CreateAsync(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Role role)
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role != null)
            {
                role.Deleted = DateTime.Now;
                _context.Roles.Update(role);
                await _context.SaveChangesAsync();
            }
        }
    }
}