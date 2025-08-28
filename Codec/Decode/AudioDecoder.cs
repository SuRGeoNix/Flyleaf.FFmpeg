namespace Flyleaf.FFmpeg.Codec.Decode;

public unsafe class AudioDecoder : AVDecoder
{
    #region Configuration Properties (RW)
    // ED VAS
    //public string?              DumpSeperator           { get => PtrToStr(ctx->dump_separator); set => av_strdup(value); }
    public ErrorDetectFlags     ErrorDetectFlags        { get => _ptr->err_recognition;              set => _ptr->err_recognition = value; }
    //public long                 MaxPixels           { get => ctx->max_pixels;                   set => ctx->max_pixels = value; } // video/subs*

    // ED VA (no S?)
    public long                 BitRate                 { get => _ptr->bit_rate;                     set => _ptr->bit_rate = value; } // (might overwritten) seems possible to set also for the decoder?
    public CodecProfile         CodecProfile            { get => GetProfile(CodecSpec.Profiles, _ptr->profile); set => _ptr->profile = value.Profile; }
    public int                  Level                   { get => _ptr->level;                        set => _ptr->level = value; }

    // ED VA 
    public StrictCompliance     StrictCompliance        { get => _ptr->strict_std_compliance;        set => _ptr->strict_std_compliance = value; }
    public int                  Threads                 { get => _ptr->thread_count;                 set => _ptr->thread_count = value; }

    // D VAS
    public int                  BitsPerCodedSample      { get => _ptr->bits_per_coded_sample;        set => _ptr->bits_per_coded_sample = value; }
    //public AVRational           PacketTimebase      { get => ctx->pkt_timebase;                 set => ctx->pkt_timebase = value; } // timebase for decoding is unused might use timebase prop name for this?
    //TODO side_data_prefer_packet

    // ED A
    public int                  SampleRate              { get => _ptr->sample_rate;                  set => _ptr->sample_rate = value; }
    public AVChannelLayout      ChannelLayout           { get => _ptr->ch_layout;                    set => _ = av_channel_layout_copy(&_ptr->ch_layout, &value); } // frees prev
    public long                 MaxSamples              { get => _ptr->max_samples;                  set => _ptr->max_samples = value; }

    // == D A ==
    public AudioDecoderFlags    Flags                   { get => (AudioDecoderFlags)_ptr->flags;     set => _ptr->flags = (CodecFlags)value; }
    public AudioDecoderFlags2   Flags2                  { get => (AudioDecoderFlags2)_ptr->flags2;   set => _ptr->flags2 = (CodecFlags2)value; }
    public AVSampleFormat       RequestSampleFormat     { get => _ptr->request_sample_fmt;           set => _ptr->request_sample_fmt = value; }

    // XX A?
    public int                  BlockAlign              { get => _ptr->block_align;                  set => _ptr->block_align = value; } // TBR: might read-only?
    //public int                  TrailPad            { get => ctx->trailing_padding;             set => ctx->trailing_padding = value; } // ED not used?
    public int                  FrameSize               { get => _ptr->frame_size;                   set => _ptr->frame_size = value; } // make sure you have this also for audio encoder (read-only)

    // D A
    public int                  InitPad                 { get => _ptr->delay;                        set => _ptr->delay = value; } // set by encoder / set to the decoder as delay instead*
    #endregion

    public FFmpegClass          AVClass                 => FFmpegClass.Get(_ptr, DA)!;
    //public FFmpegClass?         AVClassPrivate          => FFmpegClass.Get(_ptr->priv_data, DA);
    public CodecPropertyFlags   Properties              => _ptr->properties;
    public ThreadTypeFlags      ActiveThreadType        => _ptr->active_thread_type;
    public long                 FrameNumber             => _ptr->frame_num;
    public int                  BitsPerRawSample        => _ptr->bits_per_raw_sample;
    public AVSampleFormat       SampleFormat            => _ptr->sample_fmt; // we can set requested / encoders rw
    public AudioDecoderSpec     CodecSpec               { get; }
    
    // helpers
    public int                  Channels                => _ptr->ch_layout.nb_channels;

    public AudioDecoder(AudioDecoderSpec codec, AudioStream? stream = null) : base(codec)
    {
        CodecSpec = codec;

        if (stream != null)
            PrepareFrom(stream);
    }

    public void PrepareFrom(AudioStream stream)
    {
        CodecTag            = stream.CodecTag; // Note: this is not necessary related to codecId / codec
        Timebase            = stream.Timebase;

        CodecProfile        = stream.CodecProfile;
        Level               = stream.Level;

        BitRate             = stream.BitRate;
        BitsPerCodedSample  = stream.BitsPerCodedSample;

        BlockAlign          = stream.BlockAlign;
        RequestSampleFormat = stream.SampleFormat; // ffmpeg uses SampleFormat not Requested
        FrameSize           = stream.FrameSize;
        SampleRate          = stream.SampleRate;
        InitPad             = stream.InitPad;
        ChannelLayout       = stream.ChannelLayout;

        stream.ExtraDataCopyTo(&_ptr->extradata, &_ptr->extradata_size);
        stream.SideDataCopyTo(&_ptr->coded_side_data, &_ptr->nb_coded_side_data);
    }

    public FFmpegResult RecvFrame(AudioFrameBase frame)
        => new(avcodec_receive_frame(_ptr, frame));

    public int GetFrameDuration(int frameBytes)
        => av_get_audio_frame_duration(_ptr, frameBytes);

}
