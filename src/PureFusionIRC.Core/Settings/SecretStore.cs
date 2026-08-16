using System.Security.Cryptography;
using System.Text;
using PureFusionIRC.Core.Models;

namespace PureFusionIRC.Core.Settings;

/// <summary>Windows DPAPI wrapper so SASL/network passwords are not stored as raw JSON strings.</summary>
public static class SecretStore
{
    private const string Prefix = "dpapi:";

    public static string Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return string.Empty;
        }

        if (!OperatingSystem.IsWindows())
        {
            return plaintext;
        }

        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(plaintext), null, DataProtectionScope.CurrentUser);
        return Prefix + Convert.ToBase64String(bytes);
    }

    public static string Unprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored))
        {
            return string.Empty;
        }

        if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return stored;
        }

        if (!OperatingSystem.IsWindows())
        {
            return stored;
        }

        try
        {
            var bytes = Convert.FromBase64String(stored[Prefix.Length..]);
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser));
        }
        catch (CryptographicException)
        {
            return string.Empty;
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }

    public static AppSettings ProtectSettings(AppSettings settings) => settings;

    public static AppSettings UnprotectSettings(AppSettings settings) => settings;

    public static NetworkProfile ProtectNetwork(NetworkProfile network)
    {
        var copy = Clone(network);
        copy.SaslPassword = Pack(copy.SaslPassword);
        copy.NickServPassword = Pack(copy.NickServPassword);
        foreach (var server in copy.Servers)
        {
            server.Password = Pack(server.Password);
        }

        return copy;
    }

    public static NetworkProfile UnprotectNetwork(NetworkProfile network)
    {
        var copy = Clone(network);
        copy.SaslPassword = Unpack(copy.SaslPassword);
        copy.NickServPassword = Unpack(copy.NickServPassword);
        foreach (var server in copy.Servers)
        {
            server.Password = Unpack(server.Password);
        }

        return copy;
    }

    private static string? Pack(string? value) =>
        string.IsNullOrEmpty(value) ? value : Protect(value);

    private static string? Unpack(string? value) =>
        string.IsNullOrEmpty(value) ? value : Unprotect(value);

    private static NetworkProfile Clone(NetworkProfile network) => new()
    {
        Id = network.Id,
        Name = network.Name,
        AutoJoin = [.. network.AutoJoin],
        NickOverride = network.NickOverride,
        SaslAccount = network.SaslAccount,
        SaslPassword = network.SaslPassword,
        NickServPassword = network.NickServPassword,
        ConnectOnStartup = network.ConnectOnStartup,
        Enabled = network.Enabled,
        Servers = network.Servers.Select(s => new ServerEntry
        {
            Host = s.Host,
            Port = s.Port,
            UseTls = s.UseTls,
            AcceptInvalidCertificates = s.AcceptInvalidCertificates,
            Password = s.Password
        }).ToList()
    };
}
