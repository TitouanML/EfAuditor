namespace EfAuditLog.Sinks.MongoDB;

public sealed class MongoAuditSinkOptions
{
    /// <summary>MongoDB connection string. e.g. mongodb://user:pass@localhost:27017</summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>Database name where audit records will be stored.</summary>
    public string DatabaseName { get; set; } = "audit";

    /// <summary>Collection name. Defaults to "audit_records".</summary>
    public string CollectionName { get; set; } = "audit_records";
}
