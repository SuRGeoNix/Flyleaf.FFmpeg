namespace Flyleaf.FFmpeg.Filter;

public unsafe class BufferSink : FilterContext
{
    public AVRational           Timebase            => InPads[0].FilterLink is AudioFilterLink link ? link.Timebase                     : AVRational.Default; //av_buffersink_get_time_base(_ptr);

    protected BufferSink(FilterGraph graph, AVFilterContext* ctx) : base(graph, ctx) { }

    protected BufferSink(FilterGraph graph, string filterName, string? name) : base(graph, filterName, false, name) { }

    public FFmpegResult RecvFrame(AVFrame* frame, BufferSinkFlags flags = BufferSinkFlags.None)
        => new(av_buffersink_get_frame_flags(_ptr, frame, flags));

    //TBR: av_buffersink_get_side_data actually access InPads[0].FilterLink side data
}

public unsafe class AudioBufferSink : BufferSink
{
    public AVSampleFormat       SampleFormat        => InPads[0].FilterLink is AudioFilterLink link ? link.SampleFormat                 : AVSampleFormat.None;
    public int                  SampleRate          => InPads[0].FilterLink is AudioFilterLink link ? link.SampleRate                   : -1;
    public int                  Channels            => InPads[0].FilterLink is AudioFilterLink link ? link.ChannelLayout.nb_channels    : 0;
    public AVChannelLayout?     ChannelLayout       => InPads[0].FilterLink is AudioFilterLink link ? link.ChannelLayout                : null;
    

    internal AudioBufferSink(FilterGraph graph, AVFilterContext* ctx) : base(graph, ctx) { }

    public AudioBufferSink(FilterGraph graph, AudioBufferSinkParams param, string? name = null, bool initialize = true) : base(graph, "abuffersink", name)
    {
        SetParameters(param);
        
        if (initialize && InitFilter() < 0)
            throw new Exception($"Filter abuffersink failed to initialize");
    }

    public void SetParameters(AudioBufferSinkParams param)
    {
        if (param.SampleFormats != null)
            AVClass.Set("sample_formats",   param.SampleFormats,    AVOptionType.SampleFmt).    ThrowOnFailure();

        if (param.SampleRates != null)
            AVClass.Set("samplerates",      param.SampleRates,      AVOptionType.Int).          ThrowOnFailure();

        if (param.ChannelLayouts != null)
            AVClass.Set("channel_layouts",  param.ChannelLayouts,   AVOptionType.Chlayout).     ThrowOnFailure();
    }

    public FFmpegResult RecvFrame(AVFrame* frame, int samples)
        => new(av_buffersink_get_samples(_ptr, frame, samples));

    public FFmpegResult RecvFrame(AudioFrameBase frame, int samples)
        => new(av_buffersink_get_samples(_ptr, frame, samples));

    public FFmpegResult RecvFrame(AudioFrameBase frame, BufferSinkFlags flags = BufferSinkFlags.None)
        => new(av_buffersink_get_frame_flags(_ptr, frame, flags));

    public void SetMinMaxSamples(int samples)
        => av_buffersink_set_frame_size(_ptr, (uint)samples);
}

public unsafe class VideoBufferSink : BufferSink
{
    public AVPixelFormat    PixelFormat         => InPads[0].FilterLink is VideoFilterLink link ? link.PixelFormat          : AVPixelFormat.None;
    public AVColorSpace     ColorSpace          => InPads[0].FilterLink is VideoFilterLink link ? link.ColorSpace           : AVColorSpace.Unspecified;
    public AVColorRange     ColorRange          => InPads[0].FilterLink is VideoFilterLink link ? link.ColorRange           : AVColorRange.Unspecified;
    public AVRational       SampleAspectRatio   => InPads[0].FilterLink is VideoFilterLink link ? link.SampleAspectRatio    : AVRational.Default;
    public int              Width               => InPads[0].FilterLink is VideoFilterLink link ? link.Width                : 0;
    public int              Height              => InPads[0].FilterLink is VideoFilterLink link ? link.Height               : 0;

    internal VideoBufferSink(FilterGraph graph, AVFilterContext* ctx) : base(graph, ctx) { }

    public VideoBufferSink(FilterGraph graph, VideoBufferSinkParams? param = null, string? name = null, bool initialize = true) : base(graph, "buffersink", name)
    {
        if (param != null)
            SetParameters(param);
        
        if (initialize && InitFilter() < 0)
            throw new Exception($"Filter buffersink failed to initialize");
    }

    public void SetParameters(VideoBufferSinkParams param)
    {
        if (param.PixelFormats != null)
            AVClass.Set("pixel_formats",param.PixelFormats, AVOptionType.PixelFmt). ThrowOnFailure();
        
        if (param.ColorSpaces != null)
            AVClass.Set("colorspaces",  param.ColorSpaces,  AVOptionType.Int).      ThrowOnFailure();

        if (param.ColorRanges != null)
            AVClass.Set("colorranges",  param.ColorRanges,  AVOptionType.Int).      ThrowOnFailure();

        if (param.AlphaModes != null)
            AVClass.Set("alphamodes",  param.AlphaModes,    AVOptionType.Int).      ThrowOnFailure();
    }

    public FFmpegResult RecvFrame(VideoFrameBase frame, BufferSinkFlags flags = BufferSinkFlags.None)
        => new(av_buffersink_get_frame_flags(_ptr, frame, flags));
}
