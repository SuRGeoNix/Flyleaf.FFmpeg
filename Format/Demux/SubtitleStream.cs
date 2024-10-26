namespace Flyleaf.FFmpeg.Format.Demux;

public unsafe class SubtitleStream : MediaStream
{
    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr, DS)!;
    public CodecProfile         CodecProfile        => CodecDescriptor == null ? PROFILE_UNKNOWN : GetProfile(CodecDescriptor.Profiles, _codecpar->profile);
    public int                  Height              => _codecpar->height;
    public int                  Width               => _codecpar->width;

    internal SubtitleStream(FormatContext fmt, AVStream* stream) : base(fmt, stream) { }
}
