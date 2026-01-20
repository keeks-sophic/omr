using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BackendV3.Infrastructure.Persistence.Init;

public static class EfMigrationsBootstrapper
{
    public static async Task EnsureHistoryMatchesExistingSchemaAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose) await conn.OpenAsync(ct);

        try
        {
            var hasHistory = await TableExistsAsync(conn, "public", "__EFMigrationsHistory", ct);
            if (hasHistory) return;

            var looksProvisioned =
                await TableExistsAsync(conn, "auth", "users", ct) ||
                await TableExistsAsync(conn, "maps", "map_versions", ct) ||
                await TableExistsAsync(conn, "maps", "maps", ct);

            if (!looksProvisioned) return;

            var migrations = db.Database.GetMigrations().ToArray();
            if (migrations.Length == 0) return;

            logger.LogWarning("EF migrations history missing but schema exists; baselining migrations history.");

            await ExecuteAsync(conn, """
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);
""", ct);

            foreach (var id in migrations)
            {
                await ExecuteAsync(conn, """
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT @id, @ver
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = @id);
""", ct, cmd =>
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("ver", "10.0.0");
                });
            }
        }
        finally
        {
            if (shouldClose) await conn.CloseAsync();
        }
    }

    private static async Task<bool> TableExistsAsync(NpgsqlConnection conn, string schema, string table, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand("""
SELECT EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = @schema AND table_name = @table
);
""", conn);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is bool b && b;
    }

    private static async Task ExecuteAsync(NpgsqlConnection conn, string sql, CancellationToken ct, Action<NpgsqlCommand>? configure = null)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        configure?.Invoke(cmd);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
