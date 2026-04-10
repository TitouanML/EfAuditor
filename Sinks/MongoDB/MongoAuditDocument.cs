using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EfAuditLog.Sinks.MongoDB;

/// <summary>
/// MongoDB document shape for a persisted <see cref="EfAuditLog.Core.AuditRecord"/>.
/// </summary>
public sealed class MongoAuditDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string EntityType { get; set; } = null!;
    public object? EntityId { get; set; }
    public string Operation { get; set; } = null!;
    public string? OldData { get; set; }
    public string? NewData { get; set; }
    public DateTime Timestamp { get; set; }
}
