using System.Globalization;
using System.Text.RegularExpressions;

namespace PureFusionIRC.Core.Ident;

public static class IdentdProtocol
{
    private static readonly Regex Query = new(
        @"^\s*(\d{1,5})\s*,\s*(\d{1,5})\s*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryParseQuery(string line, out int localPort, out int remotePort)
    {
        localPort = 0;
        remotePort = 0;
        var text = line.Trim().TrimEnd('\r', '\n');
        var match = Query.Match(text);
        if (!match.Success)
        {
            return false;
        }

        localPort = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        remotePort = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        return localPort is > 0 and <= 65535 && remotePort is > 0 and <= 65535;
    }

    public static string FormatUserId(int localPort, int remotePort, string username, string os = "UNIX") =>
        localPort.ToString(CultureInfo.InvariantCulture) + ", " + remotePort.ToString(CultureInfo.InvariantCulture)
        + " : USERID : " + os + " : " + SanitizeUser(username);

    public static string FormatError(int localPort, int remotePort, string error = "INVALID-PORT") =>
        localPort.ToString(CultureInfo.InvariantCulture) + ", " + remotePort.ToString(CultureInfo.InvariantCulture)
        + " : ERROR : " + error;

    public static string SanitizeUser(string username)
    {
        var chars = (username ?? "").Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_').ToArray();
        return chars.Length == 0 ? "user" : new string(chars.Length <= 32 ? chars : chars[..32]);
    }
}
