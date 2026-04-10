using EfAuditLog.Core;
using MongoDB.Driver;

namespace EfAuditLog.Sinks.MongoDB;

/// <summary>
/// <see cref="IAuditSink"/> implementation that persists audit records to MongoDB.
/// </summary>
public sealed class MongoAuditSink : IAuditSink
{
    private readonly IMongoCollection<MongoAuditDocument> _collection;

    public MongoAuditSink(IMongoCollection<MongoAuditDocument> collection)
    {
        _collection = collection;
    }

    public async Task PersistAsync(IReadOnlyList<AuditRecord> records, CancellationToken cancellationToken = default)
    {
        var documents = records.Select(r => new MongoAuditDocument
        {
            EntityType = r.EntityType,
            EntityId   = r.EntityId,
            Operation  = r.Operation,
            OldData    = r.OldData,
            NewData    = r.NewData,
            Timestamp  = r.Timestamp
        }).ToList();

        await _collection.InsertManyAsync(documents, cancellationToken: cancellationToken);
    }
}
