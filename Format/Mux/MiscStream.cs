namespace Flyleaf.FFmpeg.Format.Mux;

public unsafe class DataStreamMux : MediaStreamMux
{
    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr, E)!;

    public DataStreamMux(Muxer muxer, AVCodecID codecId) : base(muxer, AVMediaType.Data, codecId) { }
}

public unsafe class AttachmentStreamMux : MediaStreamMux
{
    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr, E)!;

    public AttachmentStreamMux(Muxer muxer, AVCodecID codecId) : base(muxer, AVMediaType.Attachment, codecId) { }
}

public unsafe class UnknownStreamMux : MediaStreamMux
{
    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr, E)!;

    public UnknownStreamMux(Muxer muxer, AVCodecID codecId) : base(muxer, AVMediaType.Unknown, codecId) { }
}
