using MyCareHub.Backend.Entities;

namespace MyCareHub.Backend.Repositories.Interfaces;

public interface IAuditLogRepository
{
    Task<AuditLogEntry?> CreateAsync(AuditLogEntry entry);
    Task<IEnumerable<AuditLogEntry>> GetRecentAsync(int limit = 200);
}
