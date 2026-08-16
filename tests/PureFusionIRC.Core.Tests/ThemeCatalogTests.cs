using PureFusionIRC.Core.Theming;

namespace PureFusionIRC.Core.Tests;

public class ThemeCatalogTests
{
    [Fact]
    public void Seed_does_not_overwrite_an_existing_user_file()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pf-theme-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var catalog = new ThemeCatalog(dir);
            catalog.SeedUserCopies();
            var path = catalog.PathFor("amoled-black");
            var first = File.ReadAllText(path);
            File.WriteAllText(path, first.Replace("AMOLED Black", "My OLED", StringComparison.Ordinal));
            catalog.SeedUserCopies();
            Assert.Contains("My OLED", File.ReadAllText(path), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void CloneAsNew_writes_a_distinct_id()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pf-theme-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var catalog = new ThemeCatalog(dir);
            catalog.SeedUserCopies();
            var copy = catalog.CloneAsNew(BuiltInThemes.AmoledBlack, "Night Desk");
            Assert.Equal("night-desk", copy.Id);
            Assert.NotEqual("amoled-black", copy.Id);
            Assert.True(File.Exists(catalog.PathFor(copy.Id)));
            Assert.False(catalog.Delete("amoled-black"));
            Assert.True(catalog.Delete(copy.Id));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Slug_collapses_punctuation()
    {
        Assert.Equal("my-theme", ThemeCatalog.Slug("My Theme!"));
        Assert.Equal("custom", ThemeCatalog.Slug("!!!"));
    }
}
