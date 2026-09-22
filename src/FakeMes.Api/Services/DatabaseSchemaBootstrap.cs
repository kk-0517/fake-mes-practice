using FakeMes.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FakeMes.Api.Services;

/// <summary>
/// EnsureCreated 不会给已有库加新表，这里补建维护相关表。
/// </summary>
public static class DatabaseSchemaBootstrap
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('TrackRecords', 'Time') IS NOT NULL
               AND NOT EXISTS (
                   SELECT 1 FROM sys.indexes WHERE name = N'IX_TrackRecords_Time' AND object_id = OBJECT_ID(N'dbo.TrackRecords'))
            BEGIN
                CREATE INDEX [IX_TrackRecords_Time] ON [TrackRecords] ([Time]);
            END
            """,
            ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dbo.TrackRecordArchives', N'U') IS NULL
            BEGIN
                CREATE TABLE [TrackRecordArchives] (
                    [Id] bigint NOT NULL IDENTITY,
                    [OriginalId] int NOT NULL,
                    [StationCode] nvarchar(32) NOT NULL,
                    [Barcode] nvarchar(64) NOT NULL,
                    [Type] int NOT NULL,
                    [Time] datetime2 NOT NULL,
                    [ArchivedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_TrackRecordArchives] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_TrackRecordArchives_Barcode_Time] ON [TrackRecordArchives] ([Barcode], [Time]);
                CREATE INDEX [IX_TrackRecordArchives_ArchivedAt] ON [TrackRecordArchives] ([ArchivedAt]);
            END
            """,
            ct);

        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dbo.MaintenanceLogs', N'U') IS NULL
            BEGIN
                CREATE TABLE [MaintenanceLogs] (
                    [Id] int NOT NULL IDENTITY,
                    [StartedAt] datetime2 NOT NULL,
                    [FinishedAt] datetime2 NOT NULL,
                    [Trigger] nvarchar(32) NOT NULL,
                    [ArchivedCount] int NOT NULL,
                    [DeletedFromHotCount] int NOT NULL,
                    [Message] nvarchar(512) NOT NULL,
                    CONSTRAINT [PK_MaintenanceLogs] PRIMARY KEY ([Id])
                );
            END
            """,
            ct);
    }
}
