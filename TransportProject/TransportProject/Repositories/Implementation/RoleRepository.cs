using Microsoft.EntityFrameworkCore;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;

namespace TransportProject.Repositories.Implementation
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;
        public RoleRepository(ApplicationDbContext context) { _context = context; }

        public async Task<Role?> GetByIdAsync(int id)
        {
            try { return await _context.Roles.FirstOrDefaultAsync(x => x.Id == id && x.Deleted == null && x.IsActive); }
            catch (Exception ex) { throw new Exception("Error fetching role", ex); }
        }

        public async Task<Role?> GetByNameAsync(string name)
        {
            try { return await _context.Roles.FirstOrDefaultAsync(x => x.Name == name && x.Deleted == null && x.IsActive); }
            catch (Exception ex) { throw new Exception("Error fetching role by name", ex); }
        }
    }
}
