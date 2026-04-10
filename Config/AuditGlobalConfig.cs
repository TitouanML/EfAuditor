namespace EfAuditLog.Config;

[Flags]
public enum AuditGlobalConfig
{
    None   = 0,
    Insert = 1,
    Update = 2,
    Delete = 4,
    All    = Insert | Update | Delete
}
