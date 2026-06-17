using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace TransportProject.Repositories.Implementation
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context,
            IHttpContextAccessor http, ILogger<AuditService> logger)
        {
            _context = context;
            _http = http;
            _logger = logger;
        }

        public async Task LogAsync(string action, string? entityType = null,
            string? entityIds = null, bool success = true, string? details = null)
        {
            try
            {
                var user = _http.HttpContext?.User;
                var entry = new AuditLog
                {
                    UserName = user?.Identity?.Name ?? "Anonymous",
                    UserId = user?.FindFirst("sub")?.Value
                             ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    Action = action,
                    EntityType = entityType,
                    EntityIds = entityIds,
                    IpAddress = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                    Success = success,
                    Details = details,
                    TimestampUtc = DateTime.UtcNow
                };

                _context.Set<AuditLog>().Add(entry);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Never let audit failure break the request, but DO surface it.
                _logger.LogError(ex, "Failed to write audit log for action {Action}", action);
            }
        }
    }
}