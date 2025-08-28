namespace Flyleaf.FFmpeg.Filter;

public unsafe abstract class FilterLink
{
    public AVRational           Timebase        => _ptr->time_base;
    
    public FilterPadIn          InPad           { get; }
    public FilterPadOut         OutPad          { get; }

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

    protected FilterLink(AVFilterLink* link, FilterPadIn inPad, FilterPadOut outPad)
    {
        InPad   = inPad;
        OutPad  = outPad;
        _ptr    = link;
    }

    public readonly AVFilterLink* _ptr;

    internal static FilterLink Get(AVFilterLink* link, FilterPadIn inPad, FilterPadOut outPad)
    {
        if (link->type == AVMediaType.Audio)
            return new AudioFilterLink(link, inPad, outPad);
        else if (link->type == AVMediaType.Video)
            return new VideoFilterLink(link, inPad, outPad);

        throw new Exception($"Unexpected link media type {link->type}"); // TBR (all links are audio or video)
    }

    internal void Unlink()
        => InPad.FilterLink = OutPad.FilterLink = null;
}

public unsafe class AudioFilterLink : FilterLink
{
    public AudioFilterLink(AVFilterLink* link, FilterPadIn inPad, FilterPadOut outPad) : base(link, inPad, outPad) { }

    public AVSampleFormat       SampleFormat        => (AVSampleFormat)_ptr->format;
    public int                  SampleRate          => _ptr->sample_rate;
    public AVChannelLayout      ChannelLayout       => _ptr->ch_layout;
}

public unsafe class VideoFilterLink : FilterLink
{
    public VideoFilterLink(AVFilterLink* link, FilterPadIn inPad, FilterPadOut outPad) : base(link, inPad, outPad) { }

    public AVPixelFormat        PixelFormat         => (AVPixelFormat)_ptr->format;
    public int                  Width               => _ptr->w;
    public int                  Height              => _ptr->h;
    public AVRational           SampleAspectRatio   => _ptr->sample_aspect_ratio;
    public AVColorSpace         ColorSpace          => _ptr->colorspace;
    public AVColorRange         ColorRange          => _ptr->color_range;
    public HWFramesContextView? HWFramesContext     { get { var hwframes = avfilter_link_get_hw_frames_ctx(_ptr); return hwframes != null ? new(hwframes) : null; } }
}
