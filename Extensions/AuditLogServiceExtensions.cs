using EfAuditLog.Config;
using EfAuditLog.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EfAuditLog.Extensions;

public static class AuditLogServiceExtensions
{
    /// <summary>
    /// Registers EfAuditLog into the DI container.
    /// <code>
    /// // With a custom sink type:
    /// services.AddEfAuditLog(options => options
    ///     .LogAll()
    ///     .AddLogger&lt;OrderAuditLogger&gt;()
    ///     .UseSink&lt;PgAuditSink&gt;());
    ///
    /// // With the MongoDB connector:
    /// services.AddEfAuditLog(options => options
    ///     .LogAll()
    ///     .AddLogger&lt;OrderAuditLogger&gt;()
    ///     .UseMongoDB(mongo => {
    ///         mongo.ConnectionString = "mongodb://localhost:27017";
    ///         mongo.DatabaseName     = "audit";
    ///     }));
    /// </code>
    /// </summary>
    public static IServiceCollection AddEfAuditLog(
        this IServiceCollection services,
        Action<AuditOptions> configure)
    {
        var options = new AuditOptions();
        configure(options);

        if (options.SinkType is null && options.SinkFactory is null)
            throw new InvalidOperationException(
                "EfAuditLog: no sink registered. Call .UseSink<T>() or a connector like .UseMongoDB(...).");

        // Settings & registry — singletons, no DB dependency
        var settings = new AuditSettings { Config = options.Config };
        var accessor = new AuditSettingsAccessor(Options.Create(settings));
        var registry = new AuditLoggerRegistry(options.Loggers);

        services.AddSingleton(accessor);
        services.AddSingleton(registry);

        // Sink registration — either via type or via connector factory
        if (options.SinkFactory is not null)
            options.SinkFactory(services);
        else
            services.Add(new ServiceDescriptor(typeof(IAuditSink), options.SinkType!, options.SinkLifetime));

        return services;
    }
}
