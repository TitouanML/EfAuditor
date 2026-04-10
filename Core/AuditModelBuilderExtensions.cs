using Microsoft.EntityFrameworkCore;

namespace EfAuditLog.Core;

public static class AuditModelBuilderExtensions
{
    /// <summary>
    /// Maps <see cref="AuditLogEntry"/> to the audit_log table.
    /// Call this inside OnModelCreating on any context that implements <see cref="IAuditableContext"/>.
    /// </summary>
    public static ModelBuilder ConfigureAuditLogEntry(
        this ModelBuilder modelBuilder,
        string tableName = "audit_log")
    {
        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable(tableName);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.EntityType).HasMaxLength(100).HasColumnName("entity_type");
            entity.Property(e => e.EntityId).HasMaxLength(50).HasColumnName("entity_id");
            entity.Property(e => e.Operation).HasMaxLength(10).HasColumnName("operation");
            entity.Property(e => e.OldData).HasColumnType("jsonb").HasColumnName("old_data");
            entity.Property(e => e.NewData).HasColumnType("jsonb").HasColumnName("new_data");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
        });

        return modelBuilder;
    }
}
