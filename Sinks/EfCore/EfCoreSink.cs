using EfAuditLog.Core;
using Microsoft.EntityFrameworkCore;

namespace EfAuditLog.Sinks.EfCore;

/// <summary>
/// Persists <see cref="AuditRecord"/> entries to a generic audit_log table via EF Core.
/// <typeparamref name="TContext"/> must expose a <see cref="DbSet{AuditLogEntry}"/>
/// and call modelBuilder.ConfigureAuditLogEntry() in OnModelCreating.
/// </summary>
public sealed class EfCoreSink<TContext> : IAuditSink
    where TContext : DbContext
{
    private readonly TContext _context;

    public EfCoreSink(TContext context)
    {
        _context = context;
    }

    public async Task PersistAsync(IReadOnlyList<AuditRecord> records, CancellationToken cancellationToken = default)
    {
        var entries = _context.Set<AuditLogEntry>();

        foreach (var record in records)
        {
            entries.Add(new AuditLogEntry
            {
                EntityType = record.EntityType,
                EntityId   = record.EntityId?.ToString(),
                Operation  = record.Operation,
                OldData    = record.OldData,
                NewData    = record.NewData,
                Timestamp  = record.Timestamp
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
