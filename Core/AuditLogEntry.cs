namespace EfAuditLog.Core;

/// <summary>
/// Generic audit log entity persisted by <see cref="EfAuditLog.Sinks.EfCore.EfCoreSink{TContext}"/>.
/// Add a DbSet&lt;AuditLogEntry&gt; to your context and call modelBuilder.ConfigureAuditLogEntry()
/// in OnModelCreating to map it to the audit_log table.
/// </summary>
public sealed class AuditLogEntry
{
    public int Id { get; set; }
    public string EntityType { get; set; } = null!;
    public string? EntityId { get; set; }
    public string Operation { get; set; } = null!;
    public string? OldData { get; set; }
    public string? NewData { get; set; }
    public DateTime Timestamp { get; set; }
}
