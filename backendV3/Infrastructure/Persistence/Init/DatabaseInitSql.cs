namespace BackendV3.Infrastructure.Persistence.Init;

public static class DatabaseInitSql
{
    public static readonly string[] Required = new[]
    {
        "CREATE EXTENSION IF NOT EXISTS postgis;"
    };

    public static readonly string[] Optional = Array.Empty<string>();
}
