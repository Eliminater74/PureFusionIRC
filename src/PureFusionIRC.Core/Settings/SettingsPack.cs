using System.IO.Compression;

namespace PureFusionIRC.Core.Settings;

/// <summary>Export/import a zip of settings, networks, user themes, and scripts.</summary>
public static class SettingsPack
{
    public static void Export(SettingsStore store, string zipPath)
    {
        store.EnsureLayout();
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        AddIfExists(zip, store.SettingsPath, "settings.json");
        AddIfExists(zip, store.NetworksPath, "networks.json");
        AddDirectory(zip, store.ThemesDir, "themes");
        AddDirectory(zip, store.ScriptsDir, "scripts");
    }

    public static void Import(SettingsStore store, string zipPath)
    {
        store.EnsureLayout();
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var backup = Path.Combine(store.BackupsDir, "import-backup-" + stamp + ".zip");
        Export(store, backup);

        using var zip = ZipFile.OpenRead(zipPath);
        foreach (var entry in zip.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name) || entry.FullName.Contains("..", StringComparison.Ordinal))
            {
                continue;
            }

            var destination = Path.GetFullPath(Path.Combine(store.Root, entry.FullName));
            if (!destination.StartsWith(store.Root, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, overwrite: true);
        }
    }

    private static void AddIfExists(ZipArchive zip, string path, string entryName)
    {
        if (File.Exists(path))
        {
            zip.CreateEntryFromFile(path, entryName);
        }
    }

    private static void AddDirectory(ZipArchive zip, string dir, string prefix)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(dir, file).Replace('\\', '/');
            zip.CreateEntryFromFile(file, prefix + "/" + relative);
        }
    }
}
