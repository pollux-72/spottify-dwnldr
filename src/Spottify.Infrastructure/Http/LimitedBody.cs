namespace Spottify.Infrastructure.Http;

internal static class LimitedBody
{
    /// <summary>
    /// Reads the body into memory but refuses to go beyond <paramref name="maxBytes"/>. A plain <c>GetAsync</c> would
    /// buffer whatever the server sends before any limit could apply. Use it with
    /// <see cref="HttpCompletionOption.ResponseHeadersRead"/>.
    /// </summary>
    /// <param name="tooLarge">Creates the exception to throw when the limit is exceeded.</param>
    public static async Task<MemoryStream> ReadAsync(
        HttpResponseMessage response,
        long maxBytes,
        Func<Exception> tooLarge,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is { } announced && announced > maxBytes)
        {
            throw tooLarge();
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        var buffer = new MemoryStream();
        var chunk = new byte[81_920];
        int read;
        while ((read = await source.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                await buffer.DisposeAsync().ConfigureAwait(false);
                throw tooLarge();
            }

            buffer.Write(chunk, 0, read);
        }

        buffer.Position = 0;
        return buffer;
    }
}
