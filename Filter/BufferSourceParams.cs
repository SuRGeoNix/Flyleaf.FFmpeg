using Flyleaf.FFmpeg.Codec.Decode;

namespace Flyleaf.FFmpeg.Filter;

public unsafe class BufferSourceParams : IDisposable
{
    public AVRational   Timebase    { get => _ptr->time_base;  set => _ptr->time_base = value; }
    public bool         Disposed    => _ptr == null;

    public BufferSourceParams()
        => _ptr = av_buffersrc_parameters_alloc();

    #region Frame Side Data
    public AVFrameSideData**    SideData        => _ptr->side_data;
    public int                  SideDataCount   => _ptr->nb_side_data;

    public AVFrameSideData* SideDataGet(AVFrameSideDataType type)
        => av_frame_side_data_get_c(_ptr->side_data, _ptr->nb_side_data, type);

    public FFmpegResult SideDataCopyTo(AVFrameSideData*** dstPtr, int* dstCount, FrameSideDataFlags flags = FrameSideDataFlags.None)
        => SideDataCopy(_ptr->side_data, _ptr->nb_side_data, dstPtr, dstCount, flags);

    public AVFrameSideData* SideDataNew(AVFrameSideDataType type, nuint size, FrameSideDataFlags flags = FrameSideDataFlags.None)
        => av_frame_side_data_new(&_ptr->side_data, &_ptr->nb_side_data, type, size, (uint)flags);

    public AVFrameSideData* SideDataAdd(AVFrameSideDataType type, AVBufferRef** buffer, FrameSideDataFlags flags = FrameSideDataFlags.None)
        => av_frame_side_data_add(&_ptr->side_data, &_ptr->nb_side_data, type, buffer, (uint)flags);

    public void SideDataRemove(AVFrameSideDataType type)
        => av_frame_side_data_remove(&_ptr->side_data, &_ptr->nb_side_data, type);

    public void SideDataRemoveByProps(AVSideDataProps props)
        => av_frame_side_data_remove_by_props(&_ptr->side_data, &_ptr->nb_side_data, props);

    public void SideDataFree()
        => av_frame_side_data_free(&_ptr->side_data, &_ptr->nb_side_data);
    #endregion

    #region Disposal
    public readonly AVBufferSrcParameters* _ptr;

    public static implicit operator AVBufferSrcParameters*(BufferSourceParams bsp)
        => bsp._ptr;

    ~BufferSourceParams()
    {
        if (!Disposed)
            Free();
    }

    public void Dispose()  // maybe just let GC finalizer do it here no need for public dispose (small data)
    { 
        if (!Disposed)
        {
            Free();
            GC.SuppressFinalize(this);
        }
    }

    static System.Reflection.FieldInfo ptrField = typeof(BufferSourceParams).GetField(nameof(_ptr))!;
    void Free()
    {
        av_free(_ptr);
        ptrField.SetValue(this, null);
    }
    #endregion
}

public unsafe class AudioBufferSourceParams : BufferSourceParams
{
    public AVChannelLayout      ChannelLayout       { get => _ptr->ch_layout;               set => _ = av_channel_layout_copy(&_ptr->ch_layout, &value); } // frees prev
    public AVSampleFormat       SampleFormat        { get => (AVSampleFormat)_ptr->format;  set => _ptr->format = (int)value; }
    public int                  SampleRate          { get => _ptr->sample_rate;             set => _ptr->sample_rate = value; }

    public AudioBufferSourceParams() : base() { }
    public AudioBufferSourceParams(AudioDecoder decoder) : base()
        => PrepareFrom(decoder);

    public void PrepareFrom(AudioDecoder decoder)
    {
        Timebase        = decoder.Timebase;

        SampleFormat    = decoder.SampleFormat;
        SampleRate      = decoder.SampleRate;
        ChannelLayout   = decoder.ChannelLayout;
    }
}

public unsafe class VideoBufferSourceParams : BufferSourceParams
{
    public AVPixelFormat        PixelFormat         { get => (AVPixelFormat)_ptr->format;   set => _ptr->format = (int)value; }
    public int                  Width               { get => _ptr->width;                   set => _ptr->width = value; }
    public int                  Height              { get => _ptr->height;                  set => _ptr->height = value; }
    public AVRational           SampleAspectRatio   { get => _ptr->sample_aspect_ratio;     set => _ptr->sample_aspect_ratio = value;}
    public AVColorSpace         ColorSpace          { get => _ptr->color_space;             set => _ptr->color_space = value; }
    public AVColorRange         ColorRange          { get => _ptr->color_range;             set => _ptr->color_range = value; }
    public AVRational           FrameRate           { get => _ptr->frame_rate;              set => _ptr->frame_rate = value; }
    public HWFramesContextBase? HWFramesContext     { get => _ptr->hw_frames_ctx == null ? null : new HWFramesContextView(_ptr->hw_frames_ctx); set => _ptr->hw_frames_ctx = value; } // ffmpeg keeps ref (check what we return get=>)

    public VideoBufferSourceParams() : base() { }
    public VideoBufferSourceParams(VideoDecoder decoder) : base()
        => PrepareFrom(decoder);

    public void PrepareFrom(VideoDecoder decoder)
    {
        Timebase            = decoder.Timebase;

        Width               = decoder.Width;
        Height              = decoder.Height;
        SampleAspectRatio   = decoder.SampleAspectRatio;
        ColorSpace          = decoder.ColorSpace;
        ColorRange          = decoder.ColorRange;
        FrameRate           = decoder.FrameRate;

        if (decoder.HWFramesContext != null)
        {
            HWFramesContext = decoder.HWFramesContext;
            PixelFormat     = HWFramesContext.HWPixelFormat;
        }
        else
            PixelFormat     = decoder.PixelFormat;
    }
}
