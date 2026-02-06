using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BackendV3.Infrastructure.Persistence.Init;

public static class MapsMultiMapMigration
{
    public static async Task ApplyAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("Maps migration: checking multi-map schema");
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose) await conn.OpenAsync(ct);

        try
        {
            await EnsureMapsTableAsync(conn, ct);
            await EnsureMapVersionsColumnsAsync(conn, logger, ct);

            var hasName = await ColumnExistsAsync(conn, "maps", "map_versions", "Name", ct);
            var hasIsActive = await ColumnExistsAsync(conn, "maps", "map_versions", "IsActive", ct);

            await EnsureMapIndexesAsync(conn, ct);

            if (hasName)
            {
                await CreateMapsFromLegacyNamesAsync(conn, ct);
                await AssignMapIdFromLegacyNamesAsync(conn, ct);
            }

            await EnsureMapIdForOrphansAsync(conn, logger, ct);
            await EnsureStatusAsync(conn, hasIsActive, ct);
            await RenumberVersionsPerMapAsync(conn, ct);
            await EnsureIndexesAfterRenumberAsync(conn, ct);
            await UpdateMapsPointersAsync(conn, hasName, hasIsActive, ct);
            logger.LogInformation("Maps migration: multi-map schema ensured");
        }
        finally
        {
            if (shouldClose) await conn.CloseAsync();
        }
    }

    private static async Task EnsureMapsTableAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var sql = """
CREATE TABLE IF NOT EXISTS maps.maps (
    "MapId" uuid NOT NULL,
    "Name" text NOT NULL,
    "CreatedBy" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ArchivedAt" timestamp with time zone NULL,
    "ActivePublishedMapVersionId" uuid NULL,
    CONSTRAINT "PK_maps" PRIMARY KEY ("MapId")
);
""";
        await ExecuteAsync(conn, sql, ct);
    }

    private static async Task EnsureMapVersionsColumnsAsync(NpgsqlConnection conn, ILogger logger, CancellationToken ct)
    {
        await ExecuteAsync(conn, """ALTER TABLE maps.map_versions ADD COLUMN IF NOT EXISTS "MapId" uuid;""", ct);
        await ExecuteAsync(conn, """ALTER TABLE maps.map_versions ADD COLUMN IF NOT EXISTS "Status" text;""", ct);
        await ExecuteAsync(conn, """ALTER TABLE maps.map_versions ADD COLUMN IF NOT EXISTS "PublishedBy" uuid;""", ct);
        await ExecuteAsync(conn, """ALTER TABLE maps.map_versions ADD COLUMN IF NOT EXISTS "DerivedFromMapVersionId" uuid;""", ct);
        await ExecuteAsync(conn, """ALTER TABLE maps.map_versions ADD COLUMN IF NOT EXISTS "Label" text;""", ct);

        if (await ColumnExistsAsync(conn, "maps", "map_versions", "Name", ct))
        {
            var before = await GetIsNullableAsync(conn, "maps", "map_versions", "Name", ct);
            await ExecuteAsync(conn, """ALTER TABLE maps.map_versions ALTER COLUMN "Name" DROP NOT NULL;""", ct);
            var after = await GetIsNullableAsync(conn, "maps", "map_versions", "Name", ct);
            logger.LogInformation("Maps migration: map_versions.Name is_nullable {Before} -> {After}", before, after);
        }

        if (await ColumnExistsAsync(conn, "maps", "map_versions", "name", ct))
        {
            var before = await GetIsNullableAsync(conn, "maps", "map_versions", "name", ct);
            await ExecuteAsync(conn, """ALTER TABLE maps.map_versions ALTER COLUMN "name" DROP NOT NULL;""", ct);
            var after = await GetIsNullableAsync(conn, "maps", "map_versions", "name", ct);
            logger.LogInformation("Maps migration: map_versions.name is_nullable {Before} -> {After}", before, after);
        }

        if (await ColumnExistsAsync(conn, "maps", "map_versions", "IsActive", ct))
        {
            var before = await GetIsNullableAsync(conn, "maps", "map_versions", "IsActive", ct);
            await ExecuteAsync(conn, """ALTER TABLE maps.map_versions ALTER COLUMN "IsActive" DROP NOT NULL;""", ct);
            var after = await GetIsNullableAsync(conn, "maps", "map_versions", "IsActive", ct);
            logger.LogInformation("Maps migration: map_versions.IsActive is_nullable {Before} -> {After}", before, after);
        }

        if (await ColumnExistsAsync(conn, "maps", "map_versions", "isactive", ct))
        {
            var before = await GetIsNullableAsync(conn, "maps", "map_versions", "isactive", ct);
            await ExecuteAsync(conn, """ALTER TABLE maps.map_versions ALTER COLUMN "isactive" DROP NOT NULL;""", ct);
            var after = await GetIsNullableAsync(conn, "maps", "map_versions", "isactive", ct);
            logger.LogInformation("Maps migration: map_versions.isactive is_nullable {Before} -> {After}", before, after);
        }
    }

    private static async Task EnsureMapIndexesAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        await ExecuteAsync(conn, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_maps_Name" ON maps.maps ("Name");""", ct);
        await ExecuteAsync(conn, """CREATE INDEX IF NOT EXISTS "IX_maps_ActivePublishedMapVersionId" ON maps.maps ("ActivePublishedMapVersionId");""", ct);
        await ExecuteAsync(conn, """CREATE INDEX IF NOT EXISTS "IX_maps_ArchivedAt" ON maps.maps ("ArchivedAt");""", ct);
    }

    private static async Task CreateMapsFromLegacyNamesAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var sql = """
INSERT INTO maps.maps ("MapId", "Name", "CreatedAt")
SELECT gen_random_uuid_fallback(), mv."Name", MIN(mv."CreatedAt")
FROM maps.map_versions mv
WHERE mv."MapId" IS NULL
GROUP BY mv."Name"
ON CONFLICT ("Name") DO NOTHING;
""";

        await EnsureUuidFallbackFunctionAsync(conn, ct);
        await ExecuteAsync(conn, sql, ct);
    }

    private static async Task AssignMapIdFromLegacyNamesAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var sql = """
UPDATE maps.map_versions mv
SET "MapId" = m."MapId"
FROM maps.maps m
WHERE mv."MapId" IS NULL AND m."Name" = mv."Name";
""";
        await ExecuteAsync(conn, sql, ct);
    }

    private static async Task EnsureMapIdForOrphansAsync(NpgsqlConnection conn, ILogger logger, CancellationToken ct)
    {
        var orphanCount = await ScalarIntAsync(conn, """SELECT COUNT(*) FROM maps.map_versions WHERE "MapId" IS NULL;""", ct);
        if (orphanCount == 0) return;

        logger.LogWarning("Maps migration: found {Count} map_versions rows without MapId; creating placeholder maps.", orphanCount);

        await EnsureUuidFallbackFunctionAsync(conn, ct);

        var sql = """
INSERT INTO maps.maps ("MapId", "Name", "CreatedAt")
SELECT gen_random_uuid_fallback(), ('Imported ' || mv."MapVersionId"::text), mv."CreatedAt"
FROM maps.map_versions mv
WHERE mv."MapId" IS NULL;

UPDATE maps.map_versions mv
SET "MapId" = m."MapId"
FROM maps.maps m
WHERE mv."MapId" IS NULL AND m."Name" = ('Imported ' || mv."MapVersionId"::text);
""";
        await ExecuteAsync(conn, sql, ct);
    }

    private static async Task EnsureStatusAsync(NpgsqlConnection conn, bool hasIsActive, CancellationToken ct)
    {
        if (hasIsActive)
        {
            await ExecuteAsync(conn, """
UPDATE maps.map_versions
SET "Status" = CASE WHEN "IsActive" THEN 'PUBLISHED' ELSE 'DRAFT' END
WHERE "Status" IS NULL;
""", ct);
            return;
        }

        await ExecuteAsync(conn, """
UPDATE maps.map_versions
SET "Status" = CASE WHEN "PublishedAt" IS NOT NULL THEN 'PUBLISHED' ELSE 'DRAFT' END
WHERE "Status" IS NULL;
""", ct);
    }

    private static async Task RenumberVersionsPerMapAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var sql = """
WITH ranked AS (
    SELECT
        mv."MapVersionId",
        ROW_NUMBER() OVER (PARTITION BY mv."MapId" ORDER BY mv."CreatedAt", mv."MapVersionId") AS rn
    FROM maps.map_versions mv
    WHERE mv."MapId" IS NOT NULL
)
UPDATE maps.map_versions mv
SET "Version" = r.rn
FROM ranked r
WHERE mv."MapVersionId" = r."MapVersionId";
""";
        await ExecuteAsync(conn, sql, ct);
    }

    private static async Task EnsureIndexesAfterRenumberAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        await ExecuteAsync(conn, """CREATE UNIQUE INDEX IF NOT EXISTS "IX_map_versions_MapId_Version" ON maps.map_versions ("MapId", "Version");""", ct);
        await ExecuteAsync(conn, """CREATE INDEX IF NOT EXISTS "IX_map_versions_MapId_Status" ON maps.map_versions ("MapId", "Status");""", ct);
        await ExecuteAsync(conn, """CREATE INDEX IF NOT EXISTS "IX_map_versions_CreatedAt" ON maps.map_versions ("CreatedAt");""", ct);
        await ExecuteAsync(conn, """CREATE INDEX IF NOT EXISTS "IX_map_versions_PublishedAt" ON maps.map_versions ("PublishedAt");""", ct);
        await ExecuteAsync(conn, """DROP INDEX IF EXISTS maps."IX_map_versions_MapId_PublishedUnique";""", ct);
        await ExecuteAsync(conn, """DROP INDEX IF EXISTS maps."IX_map_versions_MapId";""", ct);
        await ExecuteAsync(conn, """CREATE INDEX IF NOT EXISTS "IX_map_versions_MapId_Published" ON maps.map_versions ("MapId") WHERE "Status" = 'PUBLISHED';""", ct);
    }

    private static async Task UpdateMapsPointersAsync(NpgsqlConnection conn, bool hasName, bool hasIsActive, CancellationToken ct)
    {
        var hasActivePublished = await ColumnExistsAsync(conn, "maps", "maps", "ActivePublishedMapVersionId", ct);
        var hasActiveLegacy = await ColumnExistsAsync(conn, "maps", "maps", "ActiveMapVersionId", ct);
        var targetColumn = hasActivePublished ? "ActivePublishedMapVersionId" : (hasActiveLegacy ? "ActiveMapVersionId" : null);
        if (targetColumn == null) return;

        if (hasIsActive && hasName)
        {
            await ExecuteAsync(conn, $"""
UPDATE maps.maps m
SET "{targetColumn}" = v."MapVersionId"
FROM maps.map_versions v
WHERE v."MapId" = m."MapId" AND v."IsActive" = TRUE;
""", ct);
            return;
        }

        await ExecuteAsync(conn, $"""
UPDATE maps.maps m
SET "{targetColumn}" = v."MapVersionId"
FROM (
    SELECT DISTINCT ON ("MapId")
        "MapId",
        "MapVersionId"
    FROM maps.map_versions
    WHERE "Status" = 'PUBLISHED'
    ORDER BY "MapId", COALESCE("PublishedAt", "CreatedAt") DESC
) v
WHERE m."MapId" = v."MapId";
""", ct);
    }

    private static async Task EnsureUuidFallbackFunctionAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var sql = """
CREATE OR REPLACE FUNCTION gen_random_uuid_fallback()
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    v text;
BEGIN
    BEGIN
        RETURN gen_random_uuid();
    EXCEPTION WHEN undefined_function THEN
        v := md5(random()::text || clock_timestamp()::text);
        RETURN (
            substr(v, 1, 8) || '-' ||
            substr(v, 9, 4) || '-' ||
            substr(v, 13, 4) || '-' ||
            substr(v, 17, 4) || '-' ||
            substr(v, 21, 12)
        )::uuid;
    END;
END;
$$;
""";
        await ExecuteAsync(conn, sql, ct);
    }

    private static async Task<bool> ColumnExistsAsync(NpgsqlConnection conn, string schema, string table, string column, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand("""
SELECT EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_schema = @schema AND table_name = @table AND column_name = @column
);
""", conn);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        cmd.Parameters.AddWithValue("column", column);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is bool b && b;
    }

    private static async Task<bool?> GetIsNullableAsync(NpgsqlConnection conn, string schema, string table, string column, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand("""
SELECT is_nullable
FROM information_schema.columns
WHERE table_schema = @schema AND table_name = @table AND column_name = @column;
""", conn);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        cmd.Parameters.AddWithValue("column", column);
        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is not string s) return null;
        return string.Equals(s, "YES", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ExecuteAsync(NpgsqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<int> ScalarIntAsync(NpgsqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result == null ? 0 : Convert.ToInt32(result);
    }
}
