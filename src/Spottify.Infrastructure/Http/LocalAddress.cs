using System.Net;
using System.Net.Sockets;

namespace Spottify.Infrastructure.Http;

/// <summary>
/// Tells whether an address stays inside the machine or the home network. Clients that put an API key into
/// the request URL accept plain <c>http</c> only for such addresses: an indexer proxy or SABnzbd next to this
/// service is normally reached over plain http there, while a key sent over the internet would be readable.
/// </summary>
internal static class LocalAddress
{
    public const string Description =
        "Plain http is only accepted for local addresses (localhost, 10.x, 172.16-31.x, 192.168.x, single-word host names, .local, .lan).";

    public static bool IsLocal(Uri uri)
    {
        var host = uri.IdnHost;

        if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address))
        {
            return IPAddress.IsLoopback(address) || IsPrivate(address);
        }

        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || !host.Contains('.', StringComparison.Ordinal) // "prowlarr", a Docker service or LAN name
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".lan", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".home.arpa", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 169 && bytes[1] == 254);
    }
}
