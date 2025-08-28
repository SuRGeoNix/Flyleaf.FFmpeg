namespace Flyleaf.FFmpeg.Codec.Decode;

public unsafe class VideoDecoder : AVDecoder
{
    #region Configuration Properties (RW)
    // ED VAS
    //public string?              DumpSeperator           { get => PtrToStr(ctx->dump_separator); set => av_strdup(value); }
    public ErrorDetectFlags     ErrorDetectFlags        { get => _ptr->err_recognition;             set => _ptr->err_recognition = value; }
    public long                 MaxPixels               { get => _ptr->max_pixels;                  set => _ptr->max_pixels = value; } // VS only?

    // ED VA (no S?)
    public long                 BitRate                 { get => _ptr->bit_rate;                    set => _ptr->bit_rate = value; } // AV (no S) ? (might overwritten) seems possible to set also for the decoder?
    public CodecProfile         CodecProfile            { get => GetProfile(CodecSpec.Profiles, _ptr->profile); set => _ptr->profile = value.Profile; } // (might overwritten)
    public int                  Level                   { get => _ptr->level;                       set => _ptr->level = value; }

    // ED VA
    public StrictCompliance     StrictCompliance        { get => _ptr->strict_std_compliance;       set => _ptr->strict_std_compliance = value; }
    public int                  Threads                 { get => _ptr->thread_count;                set => _ptr->thread_count = value; }

    // D VAS
    public int                  BitsPerCodedSample      { get => _ptr->bits_per_coded_sample;       set => _ptr->bits_per_coded_sample = value; }
    //public AVRational           PacketTimebase          { get => ctx->pkt_timebase;                 set => ctx->pkt_timebase = value; } // timebase for decoding is unused might use timebase prop name for this?

    // ED V
    public int                  Width                   { get => _ptr->width;                       set => _ptr->width = value; } // (might overwritten)
    public int                  Height                  { get => _ptr->height;                      set => _ptr->height = value; } // (might overwritten)
    public AVPixelFormat        PixelFormat             { get => _ptr->pix_fmt;                     set => _ptr->pix_fmt = value; } // (might overwritten)
    public ThreadTypeFlags      ThreadType              { get => _ptr->thread_type;                 set => _ptr->thread_type = value; }
    public IDCTAlgo             IDCTAlgo                { get => _ptr->idct_algo;                   set => _ptr->idct_algo = value; }
    public AVColorRange         ColorRange              { get => _ptr->color_range;                 set => _ptr->color_range = value; } // TBR..

    // D V
    public int                  LowRes                  { get => _ptr->lowres;                      set => _ptr->lowres = value; } // FFmpeg opts says V|A but actually is only V (related to max lowres in codecspec)
    public bool                 ApplyCropping           { get => _ptr->apply_cropping != 0;         set => _ptr->apply_cropping = value ? 1 : 0; } // TBR this affects crop_* to the frame (also check width/height coded*)
    public int                  CodedWidth              { get => _ptr->coded_width;                 set => _ptr->coded_width = value; } // (might overwritten)
    public int                  CodedHeight             { get => _ptr->coded_height;                set => _ptr->coded_height = value; } // (might overwritten)
    public AVFieldOrder         FieldOrder              { get => _ptr->field_order;                 set => _ptr->field_order = value; }
    public VideoDecoderFlags    Flags                   { get => (VideoDecoderFlags)_ptr->flags;    set => _ptr->flags = (CodecFlags)value; }
    public VideoDecoderFlags2   Flags2                  { get => (VideoDecoderFlags2)_ptr->flags2;  set => _ptr->flags2 = (CodecFlags2)value; }
    public VideoDecoderExportDataFlags
                                ExportSideDataFlags     { get => (VideoDecoderExportDataFlags)_ptr->export_side_data; set => _ptr->export_side_data = (CodecExportDataFlags)value; }
    public WorkaroundBugFlags   WorkaroundBugFlags      { get => _ptr->workaround_bugs;             set => _ptr->workaround_bugs = value; }
    public ErrorConcealmentFlags
                                ErrorConcealmentFlags   { get => _ptr->error_concealment;           set => _ptr->error_concealment = value; }
    public int                  SkipTop                 { get => _ptr->skip_top;                    set => _ptr->skip_top = value; }
    public int                  SkipBottom              { get => _ptr->skip_bottom;                 set => _ptr->skip_bottom = value; }
    public AVDiscard            SkipLoopFilter          { get => _ptr->skip_loop_filter;            set => _ptr->skip_loop_filter = value; }
    public AVDiscard            SkipIDCT                { get => _ptr->skip_idct;                   set => _ptr->skip_idct = value; }
    public AVDiscard            SkipFrame               { get => _ptr->skip_frame;                  set => _ptr->skip_frame = value; }
    public HWAccelFlags         HWAccelFlags            { get => _ptr->hwaccel_flags;               set => _ptr->hwaccel_flags = value; }
    public int                  HWExtraFrames           { get => _ptr->extra_hw_frames;             set => _ptr->extra_hw_frames = value; }
    public int                  DiscardDamagedPercentage{ get => _ptr->discard_damaged_percentage;  set => _ptr->discard_damaged_percentage = value; }
    #endregion

    public FFmpegClass          AVClass                 => FFmpegClass.Get(_ptr, DV)!;
    //public FFmpegClass?         AVClassPrivate          => FFmpegClass.Get(_ptr->priv_data, DV); // not safe
    public CodecPropertyFlags   Properties              => _ptr->properties;
    public ThreadTypeFlags      ActiveThreadType        => _ptr->active_thread_type;
    public int                  Delay                   => _ptr->delay;
    public int                  VideoDelay              => _ptr->has_b_frames;
    public long                 FrameNumber             => _ptr->frame_num;
    public int                  BitsPerRawSample        => _ptr->bits_per_raw_sample;
    public AVRational           FrameRate               => _ptr->framerate;
    public AVPixelFormat        SWPixelFormat           => _ptr->sw_pix_fmt; // DV only (will be set by ffmpeg before getformat)
    public int                  RefFrames               => _ptr->refs;
    public AVRational           SampleAspectRatio       => _ptr->sample_aspect_ratio;

    public AVColorPrimaries     ColorPrimaries          => _ptr->color_primaries;
    public AVColorTransferCharacteristic
                                ColorTransfer           => _ptr->color_trc;
    public AVColorSpace         ColorSpace              => _ptr->colorspace;
    public AVChromaLocation     ChromaLocation          => _ptr->chroma_sample_location;

    public VideoDecoderSpec     CodecSpec               { get; }

    public HWDeviceContextBase? HWDeviceContext         { get => _ptr->hw_device_ctx == null ? null : new HWDeviceContextView(_ptr->hw_device_ctx); set { if (_ptr->hw_device_ctx != null || value == null) return; _ptr->hw_device_ctx = value.RefRaw(); } } // no owner / don't overwrite (force only after getformat)
    public HWFramesContextBase? HWFramesContext         { get => _ptr->hw_frames_ctx == null ? null : new HWFramesContextView(_ptr->hw_frames_ctx); set { if (_ptr->hw_frames_ctx != null || value == null) return; _ptr->hw_frames_ctx = value.RefRaw(); } } // no owner / don't overwrite (force only after getformat)

    public AVPixelFormat GetFormatDefault(AVPixelFormat* fmt) => avcodec_default_get_format(_ptr, fmt);
    AVCodecContext_get_format?  GetFormatDlgt;

    public VideoDecoder(VideoDecoderSpec codec, VideoStream? stream = null, AVCodecContext_get_format? getFormatClbk = null, AVCodecContext_get_buffer2? getBufferClbk = null) : base(codec, getBufferClbk)
    {
        CodecSpec = codec;

        if (getFormatClbk != null)
        {
            GetFormatDlgt   = getFormatClbk;
            _ptr->get_format = GetFormatDlgt;
        }

        if (stream != null)
            PrepareFrom(stream);
    }

    public void PrepareFrom(VideoStream stream)
    {
        CodecTag            = stream.CodecTag; // Note: this is not necessary related to codecId / codec
        Timebase            = stream.Timebase;

        BitRate             = stream.BitRate;
        BitsPerCodedSample  = stream.BitsPerCodedSample;

        // FFmpeg docs seems to be wrong for those (can be set by user / even their codecpar to ctx does it)
        // Some codecs such as h264 does not check those at all (it will overwrite them) but others try to get the info if available*
        CodecProfile        = stream.CodecProfile;
        Level               = stream.Level;

        Width               = stream.Width;
        Height              = stream.Height;
        PixelFormat         = stream.PixelFormat;
        ColorRange          = stream.ColorRange;
        FieldOrder          = stream.FieldOrder;
        
        stream.ExtraDataCopyTo(&_ptr->extradata, &_ptr->extradata_size);
        stream.SideDataCopyTo(&_ptr->coded_side_data, &_ptr->nb_coded_side_data);

        // TBR: Based on Docs those should be read-only
        //SAR = stream.SAR;
        //FrameRate = stream.FrameRate;
        //ChromaLocation = stream.ChromaLocation;
        //ColorPrimaries = stream.ColorPrimaries;
        //ColorSpace = stream.ColorSpace;
        //VideoDelay = stream.VideoDelay;   
    }

    public FFmpegResult RecvFrame(VideoFrameBase frame)
        => new(avcodec_receive_frame(_ptr, frame));

    public (int widthAligned, int heightAligned) AlignDimensions(int width, int height)
    {
        avcodec_align_dimensions(_ptr, &width, &height);
        return (width, height);
    }

    public (int widthAligned, int heightAligned) AlignDimensions(int width, int height, int_array8 linesize)
    {
        avcodec_align_dimensions2(_ptr, &width, &height, ref linesize);
        return (width, height);
    }
}
