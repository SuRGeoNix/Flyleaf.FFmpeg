namespace Flyleaf.FFmpeg.Format.Demux;

public unsafe class AudioStream : MediaStream
{
    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr, A)!;

    public int                  Level               => _codecpar->level;
    public CodecProfile         CodecProfile        => CodecDescriptor == null ? PROFILE_UNKNOWN : GetProfile(CodecDescriptor.Profiles, _codecpar->profile);

    public int                  BlockAlign          => _codecpar->block_align;
    public AVChannelLayout      ChannelLayout       => _codecpar->ch_layout;
    public int                  FrameSize           => _codecpar->frame_size;
    public int                  InitPad             => _codecpar->initial_padding;
    public AVSampleFormat       SampleFormat        => (AVSampleFormat)_codecpar->format;
    public int                  SampleRate          => _codecpar->sample_rate;
    public int                  SeekPreRoll         => _codecpar->seek_preroll;
    //public int                  TrailPad            => _codecpar->trailing_padding; // Unused

    // helpers
    public int                  Channels            => _codecpar->ch_layout.nb_channels;

    internal AudioStream(FormatContext fmt, AVStream* stream) : base(fmt, stream) { }

    public int GetFrameDuration(int frameBytes)
        => av_get_audio_frame_duration2(_codecpar, frameBytes);
}
