namespace Flyleaf.FFmpeg.Format;

/// <summary>
/// IOContext is an AVIOContext (pb) representation with <see cref="Stream"/> support
/// </summary>
public unsafe partial class IOContext : IDisposable
{
    /* TBR
     * 
     * Buffer Size with config?
     * ctx->read_seek will be also set by mpv which calls avio_seek_time (only if byte seek is not possible?)
     * Set read/write/seek in constructor and avio flags for read/write etc? (currently can't write)
     * Check interrupt (parse the demuxer's interrupt here) on all read/write/seek
     * 
     * 
     * Types of AVIO (Everything should be RO and set only through config or use IO Stream for callbacks*)
     * 1) From existing/opened AVIO (only for representation, should be read-only)
     * 2) From existing/opened IO Stream (to manually handle read/write/seek)
     * 3) From Url with custom interrupt callback (check read-only, that underlying URLContext is responsible only to touch*)
     * 
     * ... Add all avio methods here (read/write etc...) might separate based on purpose*
     * 
     */

    public FFmpegClass      AVClass             => FFmpegClass.Get(_ptr)!;

    // Check if callbacks have been set?
    public bool             CanRead             { get; private set; }
    public bool             CanWrite            { get; private set; } // ctx->write_flag?
    public bool             CanSeek             { get; private set; }
    public bool             CanLength           { get; private set; }

    public int              BufferSize          => _ptr->buffer_size;
    public long             BytesRead           => _ptr->bytes_read;
    public long             BytesWritten        => _ptr->bytes_written;
    public bool             Direct              => _ptr->direct != 0;        // Whether it will flush the buffer in each write (read/seek might seek also in the buffer if no direct)
    public int              ErrorCode           => _ptr->error;
    public bool             Eof                 => _ptr->eof_reached != 0;   // avio_feof rechecks dynamically?
    public int              PacketSizeMin       => _ptr->min_packet_size;
    public int              PacketSizeMax       => _ptr->max_packet_size;
    public long             Position            => _ptr->pos;                // Position in the file
    public long             Position2           => avio_tell(_ptr);          // Position in the file + buffer (in case of !direct)
    public IOSeekableFlags  Seekable            => _ptr->seekable;           // same as CanSeek?
    public Stream?          Stream              => stream;
    public long             Size                => avio_size(_ptr);          // might not supported*?

    public string?          ProtocolWhitelist   => GetString(_ptr->protocol_whitelist);
    public string?          ProtocolBlacklist   => GetString(_ptr->protocol_blacklist);

    public string?          Url                 { get; private set; } // or Stream?
    
    // Can be parsed to format context directly*
    internal AVIOInterruptCB_callback? InterruptDlgt;
    internal AVIOInterruptCB int_cb; 

    public IOContext(AVIOContext* ptr, bool owner = false)
    {
        _ptr    = ptr;
        Owner   = owner;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        if (!owner)
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    }

    public IOContext(string url, IOFlags flags = IOFlags.Read, AVIOInterruptCB_callback? interruptClbk = null, void* interruptClbkOpaque = null, Dictionary<string, string>? opts = null)
    {
        Url = url;

        if (interruptClbk != null)
        {
            InterruptDlgt = interruptClbk; // for GC?
            int_cb.opaque = interruptClbkOpaque;
            int_cb.callback = InterruptDlgt;
        }
        
        var avopts = AVDictFromDict(opts);

        fixed (AVIOInterruptCB* interruptPtr = &int_cb)
            fixed (AVIOContext** ctxPtr = &_ptr)
                new FFmpegResult(avio_open2(ctxPtr, url, flags, interruptPtr, avopts != null ? &avopts : null)).ThrowOnFailure();
    }

    public void Flush()
        => avio_flush(_ptr);

    public long Seek(long offset, IOSeekFlags whence)
        => avio_seek(_ptr, offset, whence);

    public long SeekTime(long timestamp, int streamIndex, SeekFlags flags)
        => avio_seek_time(_ptr, streamIndex, timestamp, flags);

    public int Play()
        => avio_pause(_ptr, 0); // av_read_play

    public int Pause()
        => avio_pause(_ptr, 1); // av_read_pause

    public long Skip(long offset)
        => avio_skip(_ptr, offset);

    public static string? GetUrlProtocol(string url)
        => avio_find_protocol_name(url);

    #region Disposal
    public bool Disposed    => _ptr == null; // && Owner?
    public bool Owner       { get; private set; } = true;
    
    public readonly AVIOContext* _ptr;

    public static implicit operator AVIOContext*(IOContext ctx)
        => ctx._ptr;

    ~IOContext()
    {
        if (!Disposed && Owner)
            Free();
    }

    public void Dispose()
    { 
        if (!Disposed && Owner)
        {
            Free();
            GC.SuppressFinalize(this);
        }
    }

    void Free()
    {
        if (stream != null)
        {
            if (buffer != null)
                av_free(buffer);

            if (disposeStream)
                stream.Dispose();
        }

        fixed(AVIOContext** _ptrPtr = &_ptr)
            avio_context_free(_ptrPtr);
    }
    #endregion
}
