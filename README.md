# EfAuditLog

Pluggable EF Core audit logging library. Automatically captures `INSERT`, `UPDATE` and `DELETE` events on your entities and routes them to the storage backend of your choice — PostgreSQL, MongoDB, or any custom sink.

---

## Architecture

```
Your entities change
       ↓
AuditableDbContext.SaveChangesAsync()
       ↓ snapshots ChangeTracker
IAuditLogger.Produce()        → AuditRecord  (neutral DTO, no DB dependency)
       ↓ after base.SaveChangesAsync() — real IDs assigned
IAuditSink.PersistAsync()     → writes to whatever backend you configured
```

The main DB and the audit storage are fully decoupled. Your entity loggers know nothing about where the records go.

---

## Concepts

| Type | Role | You implement? |
|---|---|---|
| `IAuditLogger` | Transforms one entity change into an `AuditRecord` | **Yes** — one per audited entity |
| `AuditRecord` | Neutral DTO: `EntityType`, `EntityId`, `Operation`, `OldData`, `NewData`, `Timestamp` | No |
| `IAuditSink` | Persists a batch of `AuditRecord` to a backend | Only for custom sinks |
| `AuditableDbContext` | Base DbContext — orchestrates snapshot + save + sink | Extend it |
| `IAuditableContext` | Interface required by `EfCoreSink<T>` | Implement on your DbContext |
| `AuditGlobalConfig` | Flags enum controlling which operations are captured | Configure via options |

---

## Quick Start

### 1. Install

```xml
<!-- EfAuditLog.csproj project reference, or NuGet once published -->
<PackageReference Include="EfAuditLog" Version="1.0.0" />
```

### 2. Create one `IAuditLogger` per entity you want to audit

The logger's only job is to transform the entity into an `AuditRecord`. It has zero knowledge of the target database.

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using EfAuditLog.Core;
using Microsoft.EntityFrameworkCore;

public sealed class OrderAuditLogger : IAuditLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    // Tells the registry which EF entity this logger handles
    public Type EntityType => typeof(Order);

    public AuditRecord? Produce(object entity, EntityState state, string? oldDataJson)
    {
        var order = (Order)entity;

        return new AuditRecord(
            EntityType: nameof(Order),
            EntityId:   order.Id,           // real DB id — assigned before Produce() is called
            Operation:  state.ToString(),   // "Added" | "Modified" | "Deleted"
            OldData:    oldDataJson,        // JSON snapshot before save (null for inserts)
            NewData:    state != EntityState.Deleted
                            ? JsonSerializer.Serialize(order, JsonOptions)
                            : null,
            Timestamp:  DateTime.UtcNow
        );
    }
}
```

> Return `null` from `Produce()` to silently skip logging for a specific change.

### 3. Extend `AuditableDbContext`

```csharp
using EfAuditLog.Config;
using EfAuditLog.Core;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : AuditableDbContext, IAuditableContext
{
    // Required by IAuditableContext / EfCoreSink
    public DbSet<AuditLogEntry> AuditLogEntries { get; set; }

    public DbSet<Order> Orders { get; set; }

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        AuditSettingsAccessor auditSettings,
        AuditLoggerRegistry auditRegistry,
        IAuditSink auditSink)
        : base(options, auditSettings, auditRegistry, auditSink) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Your entity mappings...

        // Registers the audit_log table mapping (required when using EfCoreSink)
        modelBuilder.ConfigureAuditLogEntry();

        // Optional: custom table name
        // modelBuilder.ConfigureAuditLogEntry("my_audit_log");
    }
}
```

### 4. Register in `Program.cs`

```csharp
// Register EfAuditLog before AddDbContext
services.AddEfAuditLog(options => options
    .LogAll()                        // capture Insert + Update + Delete
    .AddLogger<OrderAuditLogger>()   // one call per audited entity
    .UseEfCore<AppDbContext>());      // sink: write to the same DB via EF

// Then register your DbContext normally
services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(connectionString));
```

---

## Configuration — `AuditGlobalConfig`

Control which operations are captured with the flags enum:

```csharp
services.AddEfAuditLog(options => options
    .LogInserts()          // Insert only
    .LogUpdates()          // Update only
    .LogDeletes()          // Delete only
    .LogAll()              // Insert | Update | Delete (default)
    .LogNone()             // disable all — useful to override at runtime
    .AddLogger<OrderAuditLogger>()
    .UseEfCore<AppDbContext>());
```

The underlying enum, usable directly if needed:

```csharp
[Flags]
public enum AuditGlobalConfig
{
    None   = 0,
    Insert = 1,
    Update = 2,
    Delete = 4,
    All    = Insert | Update | Delete
}
```

---

## Sinks

### EfCore Sink — write to a relational DB via EF Core

Stores audit records in a generic `audit_log` table in whatever database your DbContext is connected to. Works with PostgreSQL, SQL Server, SQLite, etc.

#### Required interface on your DbContext

```csharp
public class AppDbContext : AuditableDbContext, IAuditableContext
{
    public DbSet<AuditLogEntry> AuditLogEntries { get; set; }
    // ...
}
```

#### Required SQL schema

```sql
-- PostgreSQL
CREATE TABLE audit_log (
    id          SERIAL PRIMARY KEY,
    entity_type VARCHAR(100)  NOT NULL,
    entity_id   VARCHAR(50),
    operation   VARCHAR(10)   NOT NULL,   -- 'Added' | 'Modified' | 'Deleted'
    old_data    JSONB,
    new_data    JSONB,
    timestamp   TIMESTAMPTZ   NOT NULL
);
```

```sql
-- SQL Server
CREATE TABLE audit_log (
    id          INT IDENTITY PRIMARY KEY,
    entity_type NVARCHAR(100) NOT NULL,
    entity_id   NVARCHAR(50),
    operation   NVARCHAR(10)  NOT NULL,
    old_data    NVARCHAR(MAX),
    new_data    NVARCHAR(MAX),
    timestamp   DATETIME2     NOT NULL
);
```

#### Custom table name

```csharp
// In OnModelCreating:
modelBuilder.ConfigureAuditLogEntry("my_audit_log");
```

#### Registration

```csharp
services.AddEfAuditLog(options => options
    .LogAll()
    .AddLogger<OrderAuditLogger>()
    .UseEfCore<AppDbContext>());
```

---

### MongoDB Sink — write to a MongoDB collection

Stores audit records as documents in a MongoDB collection. No schema to create — MongoDB creates the collection automatically on first insert.

#### Document shape

```json
{
  "_id":        "ObjectId",
  "entityType": "Order",
  "entityId":   42,
  "operation":  "Added",
  "oldData":    null,
  "newData":    "{\"id\":42,\"total\":99.99}",
  "timestamp":  "2026-04-10T12:00:00Z"
}
```

#### Registration

```csharp
services.AddEfAuditLog(options => options
    .LogAll()
    .AddLogger<OrderAuditLogger>()
    .UseMongoDB(mongo =>
    {
        mongo.ConnectionString = "mongodb://user:password@localhost:27017";
        mongo.DatabaseName     = "audit";          // created automatically if absent
        mongo.CollectionName   = "audit_records";  // created automatically if absent
    }));
```

> `IAuditableContext` is **not** required when using the MongoDB sink. Skip the `DbSet<AuditLogEntry>` and `ConfigureAuditLogEntry()` calls entirely.

#### Connection string formats

```
# Local / Docker
mongodb://localhost:27017

# With credentials
mongodb://user:password@localhost:27017

# Atlas
mongodb+srv://user:password@cluster.mongodb.net

# With auth source
mongodb://user:password@localhost:27017/?authSource=admin
```

---

### Custom Sink

Implement `IAuditSink` to route records anywhere — HTTP, file, Redis, a separate database, etc.

```csharp
public sealed class HttpAuditSink : IAuditSink
{
    private readonly HttpClient _http;

    public HttpAuditSink(HttpClient http) => _http = http;

    public async Task PersistAsync(
        IReadOnlyList<AuditRecord> records,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(records);
        await _http.PostAsync(
            "/audit",
            new StringContent(payload, Encoding.UTF8, "application/json"),
            cancellationToken);
    }
}
```

Register with `UseSink<T>()`:

```csharp
services.AddEfAuditLog(options => options
    .LogAll()
    .AddLogger<OrderAuditLogger>()
    .UseSink<HttpAuditSink>(ServiceLifetime.Scoped));

// Register HttpAuditSink itself with whatever dependencies it needs
services.AddHttpClient<HttpAuditSink>(c => c.BaseAddress = new Uri("https://audit.example.com"));
```

---

## Complete example — PgAuditor project

```
YourProject/
├── Common/
│   ├── Data/
│   │   └── AppDbContext.cs         ← extends AuditableDbContext, implements IAuditableContext
│   └── AuditLog/
│       └── Loggers/
│           ├── OrderAuditLogger.cs ← implements IAuditLogger
│           └── UserAuditLogger.cs  ← implements IAuditLogger
└── Program.cs
```

`Program.cs`:
```csharp
// 1. Register EfAuditLog with loggers and chosen sink
services.AddEfAuditLog(options => options
    .LogAll()
    .AddLogger<UserAuditLogger>()
    .AddLogger<OrderAuditLogger>()
    .UseEfCore<AppDbContext>());    // or .UseMongoDB(...) or .UseSink<MyCustomSink>()

// 2. Register your DbContext normally — EfAuditLog injects its dependencies automatically
services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(connectionString));
```

---

## Switching sinks

To switch from EfCore to MongoDB, change one line:

```csharp
// Before
.UseEfCore<AppDbContext>()

// After
.UseMongoDB(mongo => {
    mongo.ConnectionString = "mongodb://localhost:27017";
    mongo.DatabaseName     = "audit";
})
```

Your loggers do not change. Your entities do not change.

---

## Data contract — `AuditRecord`

```csharp
public sealed record AuditRecord(
    string   EntityType,   // e.g. "Order"
    object?  EntityId,     // real DB id after save
    string   Operation,    // "Added" | "Modified" | "Deleted"
    string?  OldData,      // JSON snapshot before change (null for inserts)
    string?  NewData,      // JSON snapshot after change (null for deletes)
    DateTime Timestamp     // UTC
);
```

`OldData` and `NewData` are raw JSON strings — serialize them however you like in your `IAuditLogger.Produce()` implementation.
