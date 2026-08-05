using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Repositories.Interfaces;
using static Supabase.Postgrest.Constants;

namespace MyCareHub.Backend.Repositories.Implementations;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly Supabase.Client _supabase;

    public AuditLogRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<AuditLogEntry?> CreateAsync(AuditLogEntry entry)
    {
        if (entry.Id == Guid.Empty)
            entry.Id = Guid.NewGuid();
        if (entry.CreatoAt == default)
            entry.CreatoAt = DateTimeOffset.UtcNow;

        var response = await _supabase
            .From<AuditLogEntry>()
            .Insert(entry);

        return response.Models.FirstOrDefault();
    }

    public async Task<IEnumerable<AuditLogEntry>> GetRecentAsync(int limit = 200)
    {
        var response = await _supabase
            .From<AuditLogEntry>()
            .Order(x => x.CreatoAt!, Ordering.Descending)
            .Limit(Math.Clamp(limit, 1, 500))
            .Get();

        return response.Models;
    }
}
