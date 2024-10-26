namespace Flyleaf.FFmpeg.Format.Demux;

public unsafe class DataStream : MediaStream
{
    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr)!;

    internal DataStream(FormatContext fmt, AVStream* stream) : base(fmt, stream) { }
}

public unsafe class AttachmentStream : MediaStream
{
    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr)!;

    internal AttachmentStream(FormatContext fmt, AVStream* stream) : base(fmt, stream) { }
}

public unsafe class UnknownStream : MediaStream
{
    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr)!;

    internal UnknownStream(FormatContext fmt, AVStream* stream) : base(fmt, stream) { }
}
