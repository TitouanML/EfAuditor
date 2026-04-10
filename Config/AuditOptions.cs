using EfAuditLog.Core;
using Microsoft.Extensions.DependencyInjection;

namespace EfAuditLog.Config;

/// <summary>
/// Fluent builder used inside AddEfAuditLog(options => ...).
/// </summary>
public sealed class AuditOptions
{
    internal AuditGlobalConfig Config { get; private set; } = AuditGlobalConfig.All;
    internal List<IAuditLogger> Loggers { get; } = new();

    // Either a concrete type+lifetime OR a custom factory — mutually exclusive
    internal Type? SinkType { get; private set; }
    internal ServiceLifetime SinkLifetime { get; private set; } = ServiceLifetime.Scoped;
    internal Action<IServiceCollection>? SinkFactory { get; private set; }

    public AuditOptions LogAll()     { Config = AuditGlobalConfig.All;     return this; }
    public AuditOptions LogInserts() { Config |= AuditGlobalConfig.Insert; return this; }
    public AuditOptions LogUpdates() { Config |= AuditGlobalConfig.Update; return this; }
    public AuditOptions LogDeletes() { Config |= AuditGlobalConfig.Delete; return this; }
    public AuditOptions LogNone()    { Config = AuditGlobalConfig.None;    return this; }

    /// <summary>Register a logger instance directly.</summary>
    public AuditOptions AddLogger(IAuditLogger logger)
    {
        Loggers.Add(logger);
        return this;
    }

    /// <summary>Register a logger by type — must have a parameterless constructor.</summary>
    public AuditOptions AddLogger<T>() where T : IAuditLogger, new()
    {
        Loggers.Add(new T());
        return this;
    }

    /// <summary>
    /// Register a sink by type via standard DI.
    /// Use Scoped when the sink depends on a scoped DbContext.
    /// </summary>
    public AuditOptions UseSink<TSink>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TSink : class, IAuditSink
    {
        SinkType    = typeof(TSink);
        SinkLifetime = lifetime;
        SinkFactory  = null;
        return this;
    }

    /// <summary>
    /// Register a sink via a custom factory (used internally by connector extensions like UseMongoDB).
    /// </summary>
    internal AuditOptions SetSinkFactory(Action<IServiceCollection> factory)
    {
        SinkFactory = factory;
        SinkType    = null;
        return this;
    }
}
