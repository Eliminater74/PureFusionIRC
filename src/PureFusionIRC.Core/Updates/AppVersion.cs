using System.Globalization;
using System.Text.RegularExpressions;

namespace PureFusionIRC.Core.Updates;

public readonly record struct AppVersion(int Major, int Minor, int Patch, string Pre) : IComparable<AppVersion>
{
    private static readonly Regex PreParts = new(@"^([A-Za-z]+)(\d+)$", RegexOptions.CultureInvariant);

    public bool IsPrerelease => !string.IsNullOrEmpty(Pre);

    public static bool TryParse(string? text, out AppVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var raw = text.Trim();
        if (raw.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            raw = raw[1..];
        }

        var plus = raw.IndexOf('+');
        if (plus >= 0)
        {
            raw = raw[..plus];
        }

        var dash = raw.IndexOf('-');
        var core = dash < 0 ? raw : raw[..dash];
        var pre = dash < 0 ? "" : raw[(dash + 1)..];
        var bits = core.Split('.');
        if (bits.Length < 2
            || !int.TryParse(bits[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var major)
            || !int.TryParse(bits[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor))
        {
            return false;
        }

        var patch = 0;
        if (bits.Length > 2 && !int.TryParse(bits[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out patch))
        {
            return false;
        }

        version = new AppVersion(major, minor, patch, pre);
        return true;
    }

    public static AppVersion Parse(string text) =>
        TryParse(text, out var version) ? version : throw new FormatException("Not an app version: " + text);

    public int CompareTo(AppVersion other)
    {
        var numeric = (Major, Minor, Patch).CompareTo((other.Major, other.Minor, other.Patch));
        if (numeric != 0)
        {
            return numeric;
        }

        if (Pre.Length == 0 && other.Pre.Length == 0)
        {
            return 0;
        }

        if (Pre.Length == 0)
        {
            return 1;
        }

        if (other.Pre.Length == 0)
        {
            return -1;
        }

        var left = PreParts.Match(Pre);
        var right = PreParts.Match(other.Pre);
        if (left.Success && right.Success
            && string.Equals(left.Groups[1].Value, right.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
        {
            var a = int.Parse(left.Groups[2].Value, CultureInfo.InvariantCulture);
            var b = int.Parse(right.Groups[2].Value, CultureInfo.InvariantCulture);
            return a.CompareTo(b);
        }

        return string.Compare(Pre, other.Pre, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsNewerThan(AppVersion current) => CompareTo(current) > 0;

    public override string ToString() =>
        string.IsNullOrEmpty(Pre) ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{Pre}";
}
