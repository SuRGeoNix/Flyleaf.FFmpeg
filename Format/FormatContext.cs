namespace Flyleaf.FFmpeg.Format;

public unsafe abstract class FormatContext : IDisposable
{
    public static readonly DateTime EPOCH = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // To calculate StartRealTime

    public ReadOnlyCollection<MediaChapter> Chapters        { get; } = null!;
    public ReadOnlyCollection<MediaProgram> Programs        { get; } = null!; // HLS, DASH, MPEGTS
    public ReadOnlyCollection<MediaStream>  Streams         { get; } = null!;
    public ReadOnlyCollection<StreamGroup>  StreamGroups    { get; } = null!; // MOV (fresh maybe change - mainly IAFM audio / HEIF tiles *subs?)

    // Indexes must be the same as ffmpeg!
    protected List<MediaChapter>            chapters        = [];
    protected List<MediaProgram>            programs        = [];
    protected List<MediaStream>             streams         = [];
    protected List<StreamGroup>             streamGroups    = [];
    
    public bool                             Disposed        => _ptr == null;
    public readonly AVFormatContext* _ptr;
    protected static System.Reflection.FieldInfo ptrField = typeof(FormatContext).GetField("_ptr")!;

    public static implicit operator AVFormatContext*(FormatContext ctx)
        => ctx._ptr;

    protected FormatContext()
    {
        Chapters    = new(chapters);
        Programs    = new(programs);
        Streams     = new(streams);
        StreamGroups= new(streamGroups);
    }

    protected FormatContext(AVFormatContext* ptr) : this()
        => _ptr = ptr;

    public MediaStream? GetBestStream(AVMediaType streamType)
    {
        int streamIndex = av_find_best_stream(_ptr, streamType, -1, -1, null, 0);
        if (streamIndex >= 0 && streamIndex < _ptr->nb_streams)
            return streams[streamIndex];

        return null;
    }

    public AudioStream? BestAudioStream(MediaStream? relatedStream = null)
    {
        int i = av_find_best_stream(_ptr, AVMediaType.Audio, -1, relatedStream == null ? -1 : relatedStream.Index, null, 0);
        return i < 0 ? null : (AudioStream)streams[i];
    }

    public VideoStream? BestVideoStream(MediaStream? relatedStream = null) // This does not find the best stream (should be replaced with custom)
    {
        int i = av_find_best_stream(_ptr, AVMediaType.Video, -1, relatedStream == null ? -1 : relatedStream.Index, null, 0);
        return i < 0 ? null : (VideoStream)streams[i];
    }

    public SubtitleStream? BestSubtitleStream(MediaStream? relatedStream = null)
    {
        int i = av_find_best_stream(_ptr, AVMediaType.Subtitle, -1, relatedStream == null ? -1 : relatedStream.Index, null, 0);
        return i < 0 ? null : (SubtitleStream)streams[i];
    }

    public void Dump(string? url = null, int inputId = 0)
        => av_dump_format(_ptr, inputId, url, this is Muxer ? 1 : 0);

    #region Disposal
    ~FormatContext()
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

    protected abstract void Close();
    #endregion
}
