using TransportProject.Models;

namespace TransportProject.Repositories.Interface
{
    public interface IAuditService
    {
        Task LogAsync(string action, string? entityType = null,
            string? entityIds = null, bool success = true, string? details = null);
    }
}
