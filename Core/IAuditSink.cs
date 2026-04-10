namespace EfAuditLog.Core;

/// <summary>
/// Persists <see cref="AuditRecord"/> entries to any storage backend.
/// Implement this to target Postgres, MongoDB, HTTP, etc.
/// </summary>
public interface IAuditSink
{
    Task PersistAsync(IReadOnlyList<AuditRecord> records, CancellationToken cancellationToken = default);
}
