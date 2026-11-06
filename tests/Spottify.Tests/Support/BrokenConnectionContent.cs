using System.Net;

namespace Spottify.Tests.Support;

/// <summary>A response whose headers arrived but whose connection breaks while the body is read.</summary>
internal sealed class BrokenConnectionContent : HttpContent
{
    protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new BrokenStream());

    protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new BrokenStream());

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        throw new NotSupportedException("The test reads the body as a stream.");

    protected override bool TryComputeLength(out long length)
    {
        length = -1;
        return false;
    }

    private sealed class BrokenStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new IOException("The response ended prematurely (connection reset).");

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new IOException("The response ended prematurely (connection reset).");

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
