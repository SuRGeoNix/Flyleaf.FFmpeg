namespace Flyleaf.FFmpeg.Filter;

public unsafe class BufferSink : FilterContext
{
    public AVRational           Timebase            => InPads[0].FilterLink is AudioFilterLink link ? link.Timebase                     : AVRational.Default; //av_buffersink_get_time_base(_ptr);

    protected BufferSink(FilterGraph graph, AVFilterContext* ctx) : base(graph, ctx) { }

    protected BufferSink(FilterGraph graph, string filterName, string? name) : base(graph, filterName, false, name) { }

    public FFmpegResult RecvFrame(AVFrame* frame, BufferSinkFlags flags = BufferSinkFlags.None)
        => new(av_buffersink_get_frame_flags(_ptr, frame, flags));
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
        string chs  = "";
        if (param.ChannelLayouts != null)
        {
            for (int i = 0; i < param.ChannelLayouts.Length; i++)
            {
                chs += param.ChannelLayouts[i];
                if (i != param.ChannelLayouts.Length - 1)
                    chs += "|";
            }

            AVClass.Set("ch_layouts",           chs).ThrowOnFailure();
        }
        
        if (param.SampleFormats != null)
            AVClass.Set("sample_fmts",          param.SampleFormats).ThrowOnFailure();

        if (param.SampleRates != null)
            AVClass.Set("sample_rates",         param.SampleRates).ThrowOnFailure();
        
        if (param.AllChannelCount != null)
            AVClass.Set("all_channel_counts",   param.AllChannelCount.Value).ThrowOnFailure();
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
            AVClass.Set("pix_fmts",     param.PixelFormats).ThrowOnFailure();
        
        if (param.ColorSpaces != null)
            AVClass.Set("color_spaces", param.ColorSpaces).ThrowOnFailure();

        if (param.ColorRanges != null)
            AVClass.Set("color_ranges", param.ColorRanges).ThrowOnFailure();
    }

    public FFmpegResult RecvFrame(VideoFrameBase frame, BufferSinkFlags flags = BufferSinkFlags.None)
        => new(av_buffersink_get_frame_flags(_ptr, frame, flags));
}
