using EfAuditLog.Config;
using EfAuditLog.Core;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace EfAuditLog.Sinks.MongoDB;

public static class MongoAuditServiceExtensions
{
    /// <summary>
    /// Configures EfAuditLog to persist audit records to MongoDB.
    /// <code>
    /// services.AddEfAuditLog(options => options
    ///     .LogAll()
    ///     .AddLogger&lt;OrderAuditLogger&gt;()
    ///     .UseMongoDB(mongo => {
    ///         mongo.ConnectionString = "mongodb://user:pass@localhost:27017";
    ///         mongo.DatabaseName     = "audit";
    ///         mongo.CollectionName   = "audit_records";
    ///     }));
    /// </code>
    /// </summary>
    public static AuditOptions UseMongoDB(
        this AuditOptions auditOptions,
        Action<MongoAuditSinkOptions> configure)
    {
        var sinkOptions = new MongoAuditSinkOptions();
        configure(sinkOptions);

        // Validate eagerly
        if (string.IsNullOrWhiteSpace(sinkOptions.ConnectionString))
            throw new InvalidOperationException("MongoAuditSink: ConnectionString is required.");
        if (string.IsNullOrWhiteSpace(sinkOptions.DatabaseName))
            throw new InvalidOperationException("MongoAuditSink: DatabaseName is required.");
        if (string.IsNullOrWhiteSpace(sinkOptions.CollectionName))
            throw new InvalidOperationException("MongoAuditSink: CollectionName is required.");

        // Store factory so AuditOptions.UseSink<T> doesn't need to know about Mongo internals
        auditOptions.SetSinkFactory(services =>
        {
            var client     = new MongoClient(sinkOptions.ConnectionString);
            var database   = client.GetDatabase(sinkOptions.DatabaseName);
            var collection = database.GetCollection<MongoAuditDocument>(sinkOptions.CollectionName);

            var sink = new MongoAuditSink(collection);
            services.AddSingleton<IAuditSink>(sink);
        });

        return auditOptions;
    }
}
