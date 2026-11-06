using System.Net;

namespace Spottify.Tests.Support;

/// <summary>
/// A body without a known length that is produced while it is read, like a real network response. With
/// <c>bytes &lt; 0</c> it never delivers anything (a stalled server).
/// </summary>
internal sealed class GeneratedContent(int bytes) : HttpContent
{
    private readonly GeneratingStream _stream = new(bytes);

    public int BytesWritten => _stream.Produced;

    protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(_stream);

    protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(_stream);

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        throw new NotSupportedException("The test reads the body as a stream.");

    protected override bool TryComputeLength(out long length)
    {
        length = -1;
        return false;
    }

    private sealed class GeneratingStream(int total) : Stream
    {
        public int Produced { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (total < 0)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            var count = Math.Min(buffer.Length, total - Produced);
            buffer.Span[..count].Fill((byte)'x');
            Produced += count;
            return count;
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
