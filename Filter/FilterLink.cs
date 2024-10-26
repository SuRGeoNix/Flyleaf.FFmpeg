namespace Flyleaf.FFmpeg.Filter;

public unsafe abstract class FilterLink
{
    public AVRational           Timebase        => _ptr->time_base;
    
    public FilterPadIn          InPad           { get; }
    public FilterPadOut         OutPad          { get; }

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
}
