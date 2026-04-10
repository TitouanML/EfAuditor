using EfAuditLog.Config;
using EfAuditLog.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EfAuditLog.Extensions;

public static class AuditDbContextExtensions
{
    /// <summary>
    /// Attaches the EfAuditLog interceptor to a DbContext — no inheritance required.
    /// Call this inside the AddDbContext factory after calling AddEfAuditLog.
    /// <code>
    /// services.AddDbContext&lt;AppDbContext&gt;((provider, options) =>
    /// {
    ///     options.UseNpgsql(connectionString);
    ///     options.UseEfAuditLog(provider);
    /// });
    /// </code>
    /// </summary>
    public static DbContextOptionsBuilder UseEfAuditLog(
        this DbContextOptionsBuilder options,
        IServiceProvider provider)
    {
        var settings = provider.GetRequiredService<AuditSettingsAccessor>();
        var registry = provider.GetRequiredService<AuditLoggerRegistry>();
        var sink     = provider.GetRequiredService<IAuditSink>();

        options.AddInterceptors(new AuditInterceptor(settings, registry, sink));

        return options;
    }
}
