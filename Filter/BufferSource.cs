namespace Flyleaf.FFmpeg.Filter;

public unsafe abstract class BufferSource : FilterContext
{
    public int FailedRequests => (int)av_buffersrc_get_nb_failed_requests(_ptr);

    protected BufferSource(FilterGraph graph, string filterName, bool initialize, string? name, BufferSourceParams? param = null) : base(graph, filterName, false, name)
    {
        if (param != null)
            SetParameters(param);

        if (initialize && InitFilter() < 0)
            throw new Exception($"Filter {filterName} failed to initialize");
    }

    protected BufferSource(FilterGraph graph, AVFilterContext* ctx) : base(graph, ctx) { }

    protected FFmpegResult SetParameters(BufferSourceParams param)
        => new(av_buffersrc_parameters_set(_ptr, param)); // FFmpeg stack only usage, should be free by us

    public FFmpegResult SendFrame(AVFrame* frame, AVBuffersrcFlag flags = AVBuffersrcFlag.None)
        => new(av_buffersrc_add_frame_flags(_ptr, frame, flags));

    public FFmpegResult Drain()
        => new(av_buffersrc_add_frame_flags(_ptr, null, AVBuffersrcFlag.None));

    public FFmpegResult Close(long pts, AVBuffersrcFlag flags = AVBuffersrcFlag.None)
        => new(av_buffersrc_close(_ptr, pts, flags));
}

public unsafe class AudioBufferSource : BufferSource
{
    internal AudioBufferSource(FilterGraph graph, AVFilterContext* ctx) : base(graph, ctx) { }

    public AudioBufferSource(FilterGraph graph, AudioBufferSourceParams? param = null, string? name = null, bool initialize = true) : base(graph, "abuffer", initialize, name, param) { }

    public FFmpegResult SetParameters(AudioBufferSourceParams param)
       => base.SetParameters(param);

    public FFmpegResult SendFrame(AudioFrameBase frame, AVBuffersrcFlag flags = AVBuffersrcFlag.None)
        => new(av_buffersrc_add_frame_flags(_ptr, frame, flags));

    // maybe add gets?
    //byte* kitsos = (byte*)av_malloc(200);
    //av_opt_get(this.AVFilterContext, "time_base", OptSearchFlags.Children, &kitsos);
    //var res = PtrToStr(kitsos);
}

public unsafe class VideoBufferSource : BufferSource
{
    internal VideoBufferSource(FilterGraph graph, AVFilterContext* ctx) : base(graph, ctx) { }

    public VideoBufferSource(FilterGraph graph, VideoBufferSourceParams? param = null, string? name = null, bool initialize = true) : base(graph, "buffer", initialize, name, param) { }

    public FFmpegResult SetParameters(VideoBufferSourceParams param)
       => base.SetParameters(param);

    public FFmpegResult SendFrame(VideoFrameBase frame, AVBuffersrcFlag flags = AVBuffersrcFlag.None)
        => new(av_buffersrc_add_frame_flags(_ptr, frame, flags)); // NOTE: write_frame is equivalent with KeepRef
}
