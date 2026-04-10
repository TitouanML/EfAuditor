using System.Text.Json;
using EfAuditLog.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfAuditLog.Core;

/// <summary>
/// EF Core interceptor that snapshots entity changes before save
/// and emits <see cref="AuditRecord"/> entries to <see cref="IAuditSink"/> after save.
/// Registered per DbContext scope — no inheritance required on the main context.
/// </summary>
internal sealed class AuditInterceptor : SaveChangesInterceptor
{
    private sealed record ChangeSnapshot(
        object Entity,
        EntityState State,
        Type Type,
        string? OldDataJson);

    private readonly AuditSettingsAccessor _settings;
    private readonly AuditLoggerRegistry _registry;
    private readonly IAuditSink _sink;

    // Per-interceptor-instance list — safe because interceptor is scoped
    private readonly List<ChangeSnapshot> _pending = new();

    public AuditInterceptor(
        AuditSettingsAccessor settings,
        AuditLoggerRegistry registry,
        IAuditSink sink)
    {
        _settings = settings;
        _registry = registry;
        _sink = sink;
    }

    // Before save — OriginalValues still valid, Added entities have temp IDs
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            Snapshot(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // After save — real DB IDs assigned, safe to read EntityId
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pending.Count > 0)
        {
            var records = Produce();
            _pending.Clear();

            if (records.Count > 0)
                await _sink.PersistAsync(records, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void Snapshot(DbContext context)
    {
        var config = _settings.Config;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => _registry.TryGetLogger(e.Metadata.ClrType, out _))
            .Where(e => e.State switch
            {
                EntityState.Added    => config.HasFlag(AuditGlobalConfig.Insert),
                EntityState.Modified => config.HasFlag(AuditGlobalConfig.Update),
                EntityState.Deleted  => config.HasFlag(AuditGlobalConfig.Delete),
                _                    => false
            });

        foreach (var entry in entries)
        {
            // Deduplicate: skip if already snapshotted (e.g. EF re-enters for cascade saves)
            if (_pending.Any(p => ReferenceEquals(p.Entity, entry.Entity)))
                continue;

            _pending.Add(new ChangeSnapshot(
                Entity: entry.Entity,
                State: entry.State,
                Type: entry.Metadata.ClrType,
                OldDataJson: entry.State is EntityState.Modified or EntityState.Deleted
                    ? JsonSerializer.Serialize(entry.OriginalValues.ToObject())
                    : null
            ));
        }
    }

    private List<AuditRecord> Produce()
    {
        var records = new List<AuditRecord>(_pending.Count);

        foreach (var snap in _pending)
        {
            if (!_registry.TryGetLogger(snap.Type, out var logger) || logger is null)
                continue;

            var record = logger.Produce(snap.Entity, snap.State, snap.OldDataJson);
            if (record is not null)
                records.Add(record);
        }

        return records;
    }
}
