namespace EfAuditLog.Config;

public sealed class AuditSettings
{
    public AuditGlobalConfig Config { get; set; } = AuditGlobalConfig.All;
}
