using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Spottify.Core.Abstractions;

namespace Spottify.Infrastructure.Newznab;

/// <summary>
/// Reads the RSS answer of a Newznab search. Newznab is an RSS feed with extra <c>newznab:attr</c> elements
/// (size, category, ...). XML is used rather than the optional JSON output because every indexer supports it.
/// </summary>
internal static class NewznabResponseParser
{
    private static readonly XNamespace Newznab = "http://www.newznab.com/DTD/2010/feeds/attributes/";

    /// <summary>Releases the answer lists. Items without a title or a usable download link are skipped.</summary>
    /// <exception cref="IndexerException">The answer is an error document or not a Newznab feed.</exception>
    public static IReadOnlyList<IndexerRelease> Parse(Stream xml)
    {
        var document = Load(xml);
        var root = document.Root ?? throw new IndexerException("The indexer sent an empty answer.");

        if (root.Name.LocalName == "error")
        {
            throw ErrorFor(root);
        }

        if (root.Name.LocalName != "rss")
        {
            throw new IndexerException("The indexer answer is not a Newznab feed.");
        }

        return root.Descendants("item")
            .Select(ToRelease)
            .OfType<IndexerRelease>()
            .ToList();
    }

    /// <summary>Best effort: turns an error document of a non-200 answer into the matching exception, or <c>null</c>.</summary>
    public static IndexerException? TryParseError(Stream xml)
    {
        try
        {
            var root = Load(xml).Root;
            return root is { Name.LocalName: "error" } ? ErrorFor(root) : null;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static XDocument Load(Stream xml)
    {
        // DTDs are refused: an indexer (or whoever sits between us and it) must not be able to expand entities.
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 50_000_000,
        };

        try
        {
            using var reader = XmlReader.Create(xml, settings);
            return XDocument.Load(reader);
        }
        catch (XmlException exception)
        {
            throw new IndexerException("The indexer answer is not valid XML.", exception);
        }
    }

    private static IndexerException ErrorFor(XElement error)
    {
        _ = int.TryParse((string?)error.Attribute("code"), NumberStyles.None, CultureInfo.InvariantCulture, out var code);
        var description = (string?)error.Attribute("description") ?? "no description";
        var message = $"The indexer reported error {code}: {description}";

        return code switch
        {
            >= 100 and <= 199 => new IndexerAuthenticationException(message),
            429 or 500 or 501 => new IndexerLimitReachedException(message),
            _ => new IndexerException(message),
        };
    }

    private static IndexerRelease? ToRelease(XElement item)
    {
        var title = item.Element("title")?.Value.Trim();
        var enclosure = item.Element("enclosure");

        var link = item.Element("link")?.Value.Trim();
        if (string.IsNullOrEmpty(link))
        {
            link = (string?)enclosure?.Attribute("url");
        }

        if (string.IsNullOrEmpty(title)
            || !Uri.TryCreate(link, UriKind.Absolute, out var nzbUrl)
            || nzbUrl.Scheme is not ("http" or "https"))
        {
            return null;
        }

        var attributes = item.Elements(Newznab + "attr")
            .Select(a => (Name: (string?)a.Attribute("name"), Value: (string?)a.Attribute("value")))
            .Where(a => a.Name is not null && a.Value is not null)
            .ToLookup(a => a.Name!, a => a.Value!, StringComparer.OrdinalIgnoreCase);

        return new IndexerRelease(
            title,
            ParseSize(attributes["size"].FirstOrDefault() ?? (string?)enclosure?.Attribute("length")),
            ParseDate(item.Element("pubDate")?.Value ?? attributes["usenetdate"].FirstOrDefault()),
            PickCategory(attributes["category"], item.Element("category")?.Value),
            nzbUrl);
    }

    private static long ParseSize(string? text) =>
        long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var size) ? size : 0;

    // Feeds use RFC 822 dates ("Fri, 06 Nov 2026 12:00:00 +0000"); an unknown date is reported as the minimum.
    private static DateTimeOffset ParseDate(string? text)
    {
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
        {
            return date;
        }

        // .NET rejects a date whose weekday does not match, and some indexers get the weekday wrong.
        var comma = text?.IndexOf(',', StringComparison.Ordinal) ?? -1;
        return comma >= 0
            && DateTimeOffset.TryParse(text![(comma + 1)..], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date)
                ? date
                : DateTimeOffset.MinValue;
    }

    // An item lists its parent and its sub category (3000 and 3040). The sub category says more.
    private static string PickCategory(IEnumerable<string> categories, string? fallback)
    {
        var numeric = categories.Where(c => int.TryParse(c, NumberStyles.None, CultureInfo.InvariantCulture, out _)).ToList();
        return numeric.FirstOrDefault(c => !c.EndsWith("000", StringComparison.Ordinal))
            ?? numeric.FirstOrDefault()
            ?? fallback?.Trim()
            ?? string.Empty;
    }
}
