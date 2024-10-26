namespace Flyleaf.FFmpeg.Codec.Encode;

public unsafe class AudioEncoder : AVEncoder
{
    #region Configuration Properties (RW)
    // ED VAS
    //public string?              DumpSeperator           { get => PtrToStr(ctx->dump_separator); set => av_strdup(value); }
    public ErrorDetectFlags     ErrorDetectFlags        { get => _ptr->err_recognition;             set => _ptr->err_recognition = value; }
    //public long                 MaxPixels           { get => ctx->max_pixels;                   set => ctx->max_pixels = value; } // video/subs*

    // ED VA (no S?)
    public long                 BitRate                 { get => _ptr->bit_rate;                    set => _ptr->bit_rate = value; } // unused for constant quantizer encoding
    public CodecProfile         CodecProfile            { get => GetProfile(CodecSpec.Profiles, _ptr->profile); set => _ptr->profile = value.Profile; }
    public int                  Level                   { get => _ptr->level;                       set => _ptr->level = value; }

    // ED VA
    public StrictCompliance     StrictCompliance        { get => _ptr->strict_std_compliance;       set => _ptr->strict_std_compliance = value; }
    public int                  Threads                 { get => _ptr->thread_count;                set => _ptr->thread_count = value; }

    // E VA (no S?)
    public int                  BitRateTolerance        { get => _ptr->bit_rate_tolerance;          set => _ptr->bit_rate_tolerance = value; }
    public int                  BitsPerRawSample        { get => _ptr->bits_per_raw_sample;         set => _ptr->bits_per_raw_sample = value; }
    //public AVRational           Timebase            { get => ctx->time_base;                    set => ctx->time_base = value; } // (required)
    
    // E VA
    public long                 MaxRate                 { get => _ptr->rc_max_rate;                 set => _ptr->rc_max_rate = value; }
    public long                 MinRate                 { get => _ptr->rc_min_rate;                 set => _ptr->rc_min_rate = value; }
    public int                  BufSize                 { get => _ptr->rc_buffer_size;              set => _ptr->rc_buffer_size = value; }
    public int                  GlobalQuality           { get => _ptr->global_quality;              set => _ptr->global_quality = value; }
    public int                  Trellis                 { get => _ptr->trellis;                     set => _ptr->trellis = value; }
    public int                  CompressionLevel        { get => _ptr->compression_level;           set => _ptr->compression_level = value; }

    // ED A
    public AVSampleFormat       SampleFormat            { get => _ptr->sample_fmt;                  set => _ptr->sample_fmt = value; }
    public int                  SampleRate              { get => _ptr->sample_rate;                 set => _ptr->sample_rate = value; }
    public AVChannelLayout      ChannelLayout           { get => _ptr->ch_layout;                   set => _ = av_channel_layout_copy(&_ptr->ch_layout, &value); } // frees prev
    public long                 MaxSamples              { get => _ptr->max_samples;                 set => _ptr->max_samples = value; }

    // == E A ==
    public AudioEncoderFlags    Flags                   { get => (AudioEncoderFlags)_ptr->flags;    set => _ptr->flags = (CodecFlags)value; }
    public AVAudioServiceType   AudioServiceType        { get => _ptr->audio_service_type;          set => _ptr->audio_service_type = value; }
    public VASEncoderExportDataFlags
                                ExportSideDataFlags     { get => (VASEncoderExportDataFlags)_ptr->export_side_data;
                                                                                                    set => _ptr->export_side_data = (CodecExportDataFlags)value; }
    public int                  CutOff                  { get => _ptr->cutoff;                      set => _ptr->cutoff = value; }

    // XX A?
    //public int                  BlockAlign          { get => ctx->block_align;                  set => ctx->block_align = value; }
    //public int                  TrailPad            { get => ctx->trailing_padding;             set => ctx->trailing_padding = value; } // ED not used?

    // E A
    //public int                  SeekPreRoll         { get => ctx->seek_preroll;                 set => ctx->seek_preroll = value; } // suppose to be ro for encoding but avcodec does not currently use this
    #endregion

    public FFmpegClass          AVClass                 => FFmpegClass.Get(_ptr, EA)!;
    //public FFmpegClass?         AVClassPrivate          => FFmpegClass.Get(_ptr->priv_data, EA);
    public ThreadTypeFlags      ActiveThreadType        => _ptr->active_thread_type;
    public long                 FrameNumber             => _ptr->frame_num;
    public int                  BitsPerCodedSample      => _ptr->bits_per_coded_sample;
    public int                  FrameSize               => _ptr->frame_size;
    public int                  InitPad                 => _ptr->initial_padding;
    public int                  BlockAlign              => _ptr->block_align; // TBR: if possible to set manually to the encoder?
    public AudioEncoderSpec     CodecSpec               { get; }

    public AudioEncoder(AudioEncoderSpec codec) : base(codec) { CodecSpec = codec; }

    public FFmpegResult SendFrame(AudioFrameBase frame)
        => new(avcodec_send_frame(_ptr, frame));

    public int GetFrameDuration(int frameBytes)
        => av_get_audio_frame_duration(_ptr, frameBytes);
}
