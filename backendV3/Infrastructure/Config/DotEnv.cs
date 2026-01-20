namespace BackendV3.Infrastructure.Config;

public static class DotEnv
{
    public static void Load()
    {
        var root = FindBackendV3Root();
        if (root == null) return;

        LoadFile(Path.Combine(root, ".env.local"), overrideExisting: false);
        LoadFile(Path.Combine(root, ".env"), overrideExisting: false);
    }

    private static string? FindBackendV3Root()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

        for (var i = 0; i < 8 && dir != null; i++)
        {
            var direct = Path.Combine(dir.FullName, "backendV3.csproj");
            if (File.Exists(direct)) return dir.FullName;

            var nested = Path.Combine(dir.FullName, "backendV3", "backendV3.csproj");
            if (File.Exists(nested)) return Path.Combine(dir.FullName, "backendV3");

            dir = dir.Parent;
        }

        return null;
    }

    private static void LoadFile(string filePath, bool overrideExisting)
    {
        if (!File.Exists(filePath)) return;

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("#", StringComparison.Ordinal)) continue;

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line.Substring("export ".Length).TrimStart();
            }

            var idx = line.IndexOf('=');
            if (idx <= 0) continue;

            var key = line.Substring(0, idx).Trim();
            if (key.Length == 0) continue;

            var value = line.Substring(idx + 1).Trim();
            value = Unquote(value);

            if (!overrideExisting && Environment.GetEnvironmentVariable(key) != null) continue;
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string Unquote(string value)
    {
        if (value.Length < 2) return value;

        var first = value[0];
        var last = value[^1];
        if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }
}

