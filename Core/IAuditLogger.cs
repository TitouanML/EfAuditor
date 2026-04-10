using Microsoft.EntityFrameworkCore;

namespace EfAuditLog.Core;

/// <summary>
/// Transforms an entity change into a neutral <see cref="AuditRecord"/>.
/// No database access here — production only.
/// </summary>
public interface IAuditLogger
{
    /// <summary>The EF entity CLR type this logger handles.</summary>
    Type EntityType { get; }

    /// <summary>
    /// Called after SaveChanges — entity has its real DB id.
    /// Return null to skip logging for this specific change.
    /// </summary>
    AuditRecord? Produce(object entity, EntityState state, string? oldDataJson);
}
