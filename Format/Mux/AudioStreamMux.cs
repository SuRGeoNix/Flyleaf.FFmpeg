using Flyleaf.FFmpeg.Codec.Encode;

namespace Flyleaf.FFmpeg.Format.Mux;

public unsafe class AudioStreamMux : MediaStreamMux
{
    // TBR: Seems VA only
    public int                  Level               { get => _codecpar->level;                  set => _codecpar->level = value; }
    public CodecProfile         CodecProfile        { get => CodecDescriptor == null ? PROFILE_UNKNOWN : GetProfile(CodecDescriptor.Profiles, _codecpar->profile); set => _codecpar->profile = value.Profile; }

    public int                  BlockAlign          { get => _codecpar->block_align;            set => _codecpar->block_align = value; }
    public AVChannelLayout      ChannelLayout       { get => _codecpar->ch_layout;              set => _ = av_channel_layout_copy(&_codecpar->ch_layout, &value); } // frees prev
    public int                  FrameSize           { get => _codecpar->frame_size;             set => _codecpar->frame_size = value; }
    public int                  InitPad             { get => _codecpar->initial_padding;        set => _codecpar->initial_padding = value; }
    public AVSampleFormat       SampleFormat        { get => (AVSampleFormat)_codecpar->format; set => _codecpar->format = (int)value; }
    public int                  SampleRate          { get => _codecpar->sample_rate;            set => _codecpar->sample_rate = value; }
    public int                  SeekPreRoll         { get => _codecpar->seek_preroll;           set => _codecpar->seek_preroll = value; }
    //public int                  TrailPad            { get => _codecpar->trailing_padding;       set => _codecpar->trailing_padding= value; } // unused

    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr, EA)!;
    public int                  Channels            => _codecpar->ch_layout.nb_channels;

    public AudioStreamMux(Muxer muxer, AVCodecID codecId) : base(muxer, AVMediaType.Audio, codecId) { }
    public AudioStreamMux(Muxer muxer, AudioStream stream) : base(muxer, AVMediaType.Audio, stream.CodecId)
        => PrepareFrom(stream);
    public AudioStreamMux(Muxer muxer, AudioEncoder encoder) : base(muxer, AVMediaType.Audio, encoder.CodecSpec.CodecId)
        => PrepareFrom(encoder);

    private void PrepareFrom(AudioStream stream)
    {
        BitRate             = stream.BitRate;
        BitsPerCodedSample  = stream.BitsPerCodedSample;
        BitsPerRawSample    = stream.BitsPerRawSample;
        Disposition         = stream.Disposition;

        Timebase            = stream.Timebase;
        Level               = stream.Level;
        CodecProfile        = stream.CodecProfile;
        Metadata            = stream.Metadata;

        BlockAlign          = stream.BlockAlign;
        ChannelLayout       = stream.ChannelLayout;
        FrameSize           = stream.FrameSize;
        InitPad             = stream.InitPad;
        SampleFormat        = stream.SampleFormat;
        SampleRate          = stream.SampleRate;
        SeekPreRoll         = stream.SeekPreRoll;
        //TrailPad            = stream.TrailPad; // unused

        stream.ExtraDataCopyTo(&_codecpar->extradata, &_codecpar->extradata_size);
        stream.SideDataCopyTo(&_ptr->codecpar->coded_side_data, &_ptr->codecpar->nb_coded_side_data);
    }

    private void PrepareFrom(AudioEncoder encoder)
    {
        BitRate             = encoder.BitRate;
        BitsPerCodedSample  = encoder.BitsPerCodedSample;
        BitsPerRawSample    = encoder.BitsPerRawSample;

        Timebase            = encoder.Timebase;
        Level               = encoder.Level;
        CodecProfile        = encoder.CodecProfile;

        BlockAlign          = encoder.BlockAlign;
        ChannelLayout       = encoder.ChannelLayout;
        FrameSize           = encoder.FrameSize;
        InitPad             = encoder.InitPad;
        SampleFormat        = encoder.SampleFormat;
        SampleRate          = encoder.SampleRate;

        encoder.ExtraDataCopyTo(&_codecpar->extradata, &_codecpar->extradata_size);
        encoder.SideDataCopyTo(&_ptr->codecpar->coded_side_data, &_ptr->codecpar->nb_coded_side_data);
    }

    public int GetFrameDuration(int frameBytes)
        => av_get_audio_frame_duration2(_codecpar, frameBytes);
}
