namespace EfAuditLog.Core;

/// <summary>
/// Neutral DTO produced by an <see cref="IAuditLogger"/>.
/// Has no dependency on any database technology.
/// </summary>
public sealed record AuditRecord(
    string EntityType,
    object? EntityId,
    string Operation,
    string? OldData,
    string? NewData,
    DateTime Timestamp
);
