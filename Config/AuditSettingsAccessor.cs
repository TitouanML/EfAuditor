using Microsoft.Extensions.Options;

namespace EfAuditLog.Config;

public sealed class AuditSettingsAccessor
{
    private readonly AuditSettings _settings;

    public AuditSettingsAccessor(IOptions<AuditSettings> options)
    {
        _settings = options.Value;
    }

    public AuditGlobalConfig Config => _settings.Config;
}
