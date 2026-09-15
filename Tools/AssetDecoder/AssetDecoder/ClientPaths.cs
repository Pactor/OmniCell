namespace AssetDecoder;

/// <summary>
/// Finds the client install: AO_CLIENT from the environment, else from paths.cfg at the
/// repository root, the same setting the Extractor Serializer uses. See SETUP.md.
/// </summary>
public static class ClientPaths
{
    public static string AoClient()
    {
        string value = Setting("AO_CLIENT");
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException("Set AO_CLIENT in paths.cfg to the classic client install (the folder containing cd_image).");
        }

        return value;
    }

    private static string Setting(string name)
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment.Trim();
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            string candidate = Path.Combine(directory.FullName, "paths.cfg");
            if (!File.Exists(candidate))
            {
                continue;
            }

            foreach (string line in File.ReadAllLines(candidate))
            {
                string trimmed = line.Trim();
                int equals = trimmed.IndexOf('=');
                if (trimmed.StartsWith('#') || equals < 0)
                {
                    continue;
                }

                if (string.Equals(trimmed[..equals].Trim(), name, StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed[(equals + 1)..].Trim();
                }
            }

            return string.Empty;
        }

        return string.Empty;
    }
}
