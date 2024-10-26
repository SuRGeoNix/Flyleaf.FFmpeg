namespace Flyleaf.FFmpeg.Codec;

public unsafe abstract class CodecContext : IDisposable
{
    public CodecDescriptor?     CodecDescriptor     { get; }// => FindCodecDescriptor(ctx->codec_id);
    public uint                 CodecTag            { get => _ptr->codec_tag;       set => _ptr->codec_tag = value; }  // TBR: Seems can be set for all

    // D VA (no S?)
    public byte*                ExtraData           { get => _ptr->extradata;       set => _ptr->extradata = value; }
    public int                  ExtraDataSize       { get => _ptr->extradata_size;  set => _ptr->extradata_size = value; }
    public void ExtraDataCopyTo(byte** dstPtr, int* dstSize)
        => ExtraDataCopy(_ptr->extradata, _ptr->extradata_size, dstPtr, dstSize);

    public bool                 IsOpened            => avcodec_is_open(_ptr) != 0;

    protected CodecContext(AVCodec* codec)
    {
        _ptr = avcodec_alloc_context3(codec);
        CodecDescriptor = FindCodecDescriptor(_ptr->codec_id);
    }

    public FFmpegResult Open(Dictionary<string, string>? opts = null)
    {
        if (opts == null)
            return new(avcodec_open2(_ptr, null, null));
        
        var avopts          = AVDictFromDict(opts);
        FFmpegResult ret    = new(avcodec_open2(_ptr, null, ref avopts)); // codec = null here specify only during allocate*?

        opts.Clear();

        if (avopts != null)
        {
            AVDictToDict(opts, avopts);
            AVDictFree(&avopts);
        }

        return ret;
    }

    public void Flush()
        => avcodec_flush_buffers(_ptr);

    #region Disposal
    public bool Disposed => _ptr == null;

    public readonly AVCodecContext* _ptr;

    public static implicit operator AVCodecContext*(CodecContext ctx)
        => ctx._ptr;

    ~CodecContext()
    {
        if (!Disposed)
            Close();
    }

    public void Dispose()
    { 
        if (!Disposed)
        {
            Close();
            GC.SuppressFinalize(this);
        }
    }

    protected void Close()
    {
        fixed(AVCodecContext** _ptrPtr = &_ptr)
            avcodec_free_context(_ptrPtr);
    }
    #endregion
}
