namespace EfAuditLog.Core;

public sealed class AuditLoggerRegistry
{
    private readonly Dictionary<Type, IAuditLogger> _loggers;

    public AuditLoggerRegistry(IEnumerable<IAuditLogger> loggers)
    {
        _loggers = loggers.ToDictionary(l => l.EntityType);
    }

    public bool TryGetLogger(Type type, out IAuditLogger? logger)
        => _loggers.TryGetValue(type, out logger);
}
