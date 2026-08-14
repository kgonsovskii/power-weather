using Power.Weather.Domain.Weather;

namespace Power.Weather.Providers.WeatherDotCom;

internal sealed class ProgressReportingStream(
    Stream inner,
    long? totalBytes,
    IWeatherLoadProgress progress) : Stream
{
    private long _received;

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => totalBytes ?? inner.Length;
    public override long Position
    {
        get => _received;
        set => throw new NotSupportedException();
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var read = await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        Report(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        Report(read);
        return read;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = inner.Read(buffer, offset, count);
        Report(read);
        return read;
    }

    public override void Flush() => inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private void Report(int read)
    {
        if (read <= 0)
        {
            return;
        }

        _received += read;
        var ratio = totalBytes is > 0
            ? Math.Clamp(_received / (double)totalBytes.Value, 0, 0.95)
            : Math.Min(0.9, 0.15 + (_received / 50_000.0));

        progress.Report(new WeatherLoadProgressUpdate(
            WeatherLoadPhase.Downloading,
            "Скачиваем прогноз…",
            ratio,
            _received,
            totalBytes));
    }
}
