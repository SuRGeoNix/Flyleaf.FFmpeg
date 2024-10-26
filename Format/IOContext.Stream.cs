namespace Flyleaf.FFmpeg.Format;

public unsafe partial class IOContext
{
    Stream?             stream;
    bool                disposeStream;
    byte*               buffer;

    avio_alloc_context_read_packet?     ReadPacketDlgt;
    avio_alloc_context_write_packet?    WritePacketDlgt;
    avio_alloc_context_seek?            SeekDlgt;

    /// <summary>
    /// Creates an IOContext which can be used for Mux(write) or Demux(read/seek)
    /// </summary>
    /// <param name="stream">The stream that will be used for read/write</param>
    /// <param name="bufferSize">The size of the buffer that will be used for read/write</param>
    /// <param name="disposeStream">The stream will be disposed during IOContext disposal</param>
    public IOContext(Stream stream, int bufferSize = 2 * 1024 * 1024, bool disposeStream = false)
    {
        try { CanRead   = stream.CanRead; } catch { };
        try { CanWrite  = stream.CanWrite;} catch { };
        try { CanSeek   = stream.CanSeek; } catch { };
        try { _         = stream.Length; CanLength = true; } catch { }; // 0 length?

        ReadPacketDlgt  = !stream.CanRead   ? null : ReadPacket;
        WritePacketDlgt = !stream.CanWrite  ? null : WritePacket;
        SeekDlgt        = !stream.CanSeek   ? null : CanLength ? SeekLength : Seek;

        this.stream = stream;
        this.disposeStream = disposeStream;
        buffer = (byte*)av_mallocz((nuint)bufferSize);

        // TODO set to 1 for Write (and user should decide if wants read/write)
        
        _ptr = avio_alloc_context(buffer, bufferSize, 0, _ptr, // ctx itself as opaque? can cause issues as normally expects URLContext* there? any reason to set this?
            CanRead  ? ReadPacketDlgt   : null, 
            CanWrite ? WritePacketDlgt  : null, 
            CanSeek  ? SeekDlgt         : null);
    }

    int ReadPacket(void* opaque, byte* buffer, int length)
    {
        int bytesRead = stream!.Read(new Span<byte>(buffer, length));
        return bytesRead > 0 ? bytesRead : AVERROR_EOF; // AVERROR_EXIT?
    }

    int WritePacket(void* opaque, byte* buffer, int length)
    {
        stream!.Write(new ReadOnlySpan<byte>(buffer, length));
        return length;
    }

    long Seek(void* opaque, long offset, IOSeekFlags whence)
    {
        if (whence.HasFlag(IOSeekFlags.Size))
            return -38; // AVERROR(ENOSYS) (mpv returns -1, ENOSYS is for read_seek)
        else if (whence.HasFlag(IOSeekFlags.Begin))
            return stream!.Seek(offset, SeekOrigin.Begin);
        else if (whence.HasFlag(IOSeekFlags.Current))
            return stream!.Seek(offset, SeekOrigin.Current);
        else if (whence.HasFlag(IOSeekFlags.End))
            return stream!.Seek(offset, SeekOrigin.End);
        else
            return -38; // AVSEEK_FORCE?
    }

    long SeekLength(void* opaque, long offset, IOSeekFlags whence)
    {
        if (whence.HasFlag(IOSeekFlags.Size))
            return stream!.Length;
        else if (whence.HasFlag(IOSeekFlags.Begin))
            return stream!.Seek(offset, SeekOrigin.Begin);
        else if (whence.HasFlag(IOSeekFlags.Current))
            return stream!.Seek(offset, SeekOrigin.Current);
        else if (whence.HasFlag(IOSeekFlags.End))
            return stream!.Seek(offset, SeekOrigin.End);
        else
            return -38; // AVSEEK_FORCE?
    }
}
