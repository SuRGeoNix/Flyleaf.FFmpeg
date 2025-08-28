namespace Flyleaf.FFmpeg.Codec;

public unsafe abstract class AudioFrameBase : FrameBase
{
    public AVChannelLayout      ChannelLayout           { get => _ptr->ch_layout;               set => _ = av_channel_layout_copy(&_ptr->ch_layout, &value); } // frees prev
    public AVSampleFormat       SampleFormat            { get => (AVSampleFormat)_ptr->format;  set => _ptr->format = (int)value; }
    public int                  SampleRate              { get => _ptr->sample_rate;             set => _ptr->sample_rate = value; }
    public int                  Samples                 { get => _ptr->nb_samples;              set => _ptr->nb_samples = value; }

    public byte**               DataExtended            => _ptr->extended_data;
    public AVBufferRef**        BufferRefsExtended      => _ptr->extended_buf;
    public int                  BufferRefsExtendedCount => _ptr->nb_extended_buf;

    protected AudioFrameBase() : base() { }
    protected AudioFrameBase(AVFrame* ptr) : base(ptr) { }

    public FFmpegResult CopyPropertiesTo(AudioFrameBase frame)
        => base.CopyPropertiesTo(frame);

    public AudioFrame Clone()
        => new(CloneRaw());

    public FFmpegResult Ref(AudioFrameBase frame)
        => base.Ref(frame);

    public AudioFrame Ref()
    {
        AudioFrame frame = new();
        Ref(frame);
        return frame;
    }

    public void MoveRef(AudioFrameBase frame)
        => base.MoveRef(frame);

    // TBR: This should not probably be called when we use InitBuffer (it does not use bufferRefs?) * added also in SampleUtils?
    //public int Fill(byte* buffer, int bufferSize, int align = 0) // nb_samples must be set too before calling this
    //    => avcodec_fill_audio_frame(_ptr, ChannelLayout.nb_channels, SampleFormat, buffer, bufferSize, align);

    public long GetBufferSize(bool align = true, int* linesize = null)
        => SampleFormat.GetBufferSize(ChannelLayout.nb_channels, Samples, align, linesize);
}

public unsafe sealed class AudioFrameView(AVFrame* ptr) : AudioFrameBase(ptr) { }

public unsafe sealed class AudioFrame : AudioFrameBase, IDisposable
{
    public AudioFrame() : base() { }
    public AudioFrame(AVSampleFormat sampleFormat, AVChannelLayout channelLayout, int sampleRate, int samples, int align = 0) : this()
    {
        ChannelLayout   = channelLayout;
        SampleFormat    = sampleFormat;
        SampleRate      = sampleRate;
        Samples         = samples;
        InitBuffer(align);          // same as av_samples_fill_arrays? (avcodec_fill_audio_frame?)
    }
    internal AudioFrame(AVFrame* ptr) : base(ptr) { }

    #region Disposal
    ~AudioFrame()
    {
        if (!Disposed)
            Free();
    }

    public void Dispose()
    { 
        if (!Disposed)
        {
            Free();
            GC.SuppressFinalize(this);
        }
    }

    void Free()
    {
        fixed(AVFrame** _ptrPtr = &_ptr)
            av_frame_free(_ptrPtr);
    }
    #endregion
}