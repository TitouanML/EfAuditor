using EfAuditLog.Config;
using EfAuditLog.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EfAuditLog.Sinks.EfCore;

public static class EfCoreAuditExtensions
{
    /// <summary>
    /// Persists audit records to a generic audit_log table in the given EF Core context.
    /// <typeparamref name="TContext"/> must expose a DbSet&lt;AuditLogEntry&gt;
    /// and call modelBuilder.ConfigureAuditLogEntry() in OnModelCreating.
    /// <code>
    /// services.AddEfAuditLog(options => options
    ///     .LogAll()
    ///     .AddLogger&lt;OrderAuditLogger&gt;()
    ///     .UseEfCore&lt;MyDbContext&gt;());
    /// </code>
    /// </summary>
    public static AuditOptions UseEfCore<TContext>(this AuditOptions options)
        where TContext : DbContext
    {
        options.SetSinkFactory(services =>
            services.AddScoped<IAuditSink, EfCoreSink<TContext>>());

        return options;
    }
}
