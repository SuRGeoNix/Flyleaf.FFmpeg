namespace Flyleaf.FFmpeg.Codec.Encode;

public unsafe class VideoEncoder : AVEncoder
{
    #region Configuration Properties (RW)
    // ED VAS
    //public string?              DumpSeperator           { get => PtrToStr(ctx->dump_separator); set => av_strdup(value); }
    public ErrorDetectFlags     ErrorDetectFlags        { get => _ptr->err_recognition;             set => _ptr->err_recognition = value; }
    public long                 MaxPixels               { get => _ptr->max_pixels;                  set => _ptr->max_pixels = value; } // VS only?

    // ED VA (no S?)
    public long                 BitRate                 { get => _ptr->bit_rate;                    set => _ptr->bit_rate = value; } // AV (no S) ? (might overwritten if known) seems possible to set also for the decoder?
    public CodecProfile         CodecProfile            { get => GetProfile(CodecSpec.Profiles, _ptr->profile); set => _ptr->profile = value.Profile; }
    public int                  Level                   { get => _ptr->level;                       set => _ptr->level = value; }

    // ED VA 
    public StrictCompliance     StrictCompliance        { get => _ptr->strict_std_compliance;       set => _ptr->strict_std_compliance = value; }
    public int                  Threads                 { get => _ptr->thread_count;                set => _ptr->thread_count = value; }

    // E VA (no S?)
    public int                  BitRateTolerance        { get => _ptr->bit_rate_tolerance;          set => _ptr->bit_rate_tolerance = value; }
    public int                  BitsPerRawSample        { get => _ptr->bits_per_raw_sample;         set => _ptr->bits_per_raw_sample = value; }
    //public AVRational           Timebase                { get => ctx->time_base;                    set => ctx->time_base = value; } // (required)

    // E VA
    public long                 MaxRate                 { get => _ptr->rc_max_rate;                 set => _ptr->rc_max_rate = value; }
    public long                 MinRate                 { get => _ptr->rc_min_rate;                 set => _ptr->rc_min_rate = value; }
    public int                  BufSize                 { get => _ptr->rc_buffer_size;              set => _ptr->rc_buffer_size = value; }
    public int                  GlobalQuality           { get => _ptr->global_quality;              set => _ptr->global_quality = value; }
    public int                  Trellis                 { get => _ptr->trellis;                     set => _ptr->trellis = value; }
    public int                  CompressionLevel        { get => _ptr->compression_level;           set => _ptr->compression_level = value; }

    // ED V
    public int                  Width                   { get => _ptr->width;                       set => _ptr->width = value; } // (required)
    public int                  Height                  { get => _ptr->height;                      set => _ptr->height = value; } // (required)
    public AVPixelFormat        PixelFormat             { get => _ptr->pix_fmt;                     set => _ptr->pix_fmt = value; } // (required?)
    public ThreadTypeFlags      ThreadType              { get => _ptr->thread_type;                 set => _ptr->thread_type = value; }
    public IDCTAlgo             IDCTAlgo                { get => _ptr->idct_algo;                   set => _ptr->idct_algo = value; }
    public AVColorRange         ColorRange              { get => _ptr->color_range;                 set => _ptr->color_range = value; }
    
    // E V
    public AVColorPrimaries     ColorPrimaries          { get => _ptr->color_primaries;             set => _ptr->color_primaries = value; }
    public AVColorTransferCharacteristic
                                ColorTransfer           { get => _ptr->color_trc;                   set => _ptr->color_trc = value; }
    public AVColorSpace         ColorSpace              { get => _ptr->colorspace;                  set => _ptr->colorspace = value; }
    public AVChromaLocation     ChromaLocation          { get => _ptr->chroma_sample_location;      set => _ptr->chroma_sample_location = value; }
    public AVRational           FrameRate               { get => _ptr->framerate;                   set => _ptr->framerate = value; }
    public AVFieldOrder         FieldOrder              { get => _ptr->field_order;                 set => _ptr->field_order = value; } // TBR: docs say read-only but ffmpeg tool sett his
    public VideoEncoderFlags    Flags                   { get => (VideoEncoderFlags)_ptr->flags;    set => _ptr->flags = (CodecFlags)value; }
    public VideoEncoderFlags2   Flags2                  { get => (VideoEncoderFlags2)_ptr->flags2;  set => _ptr->flags2 = (CodecFlags2)value; }
    public VASEncoderExportDataFlags
                                ExportSideDataFlags     { get => (VASEncoderExportDataFlags)_ptr->export_side_data; set => _ptr->export_side_data = (CodecExportDataFlags)value; }

    public int                  BFrames                 { get => _ptr->max_b_frames;                set => _ptr->max_b_frames = value; }
    public float                BQPFactor               { get => _ptr->b_quant_factor;              set => _ptr->b_quant_factor = value; }
    public float                BQPOffset               { get => _ptr->b_quant_offset;              set => _ptr->b_quant_offset = value; }
    public float                IQPFactor               { get => _ptr->i_quant_factor;              set => _ptr->i_quant_factor = value; }
    public float                IQPOffset               { get => _ptr->i_quant_offset;              set => _ptr->i_quant_offset = value; }
        
    public int                  GopSize                 { get => _ptr->gop_size;                    set => _ptr->gop_size = value; }
    public float                QBlur                   { get => _ptr->qblur;                       set => _ptr->qblur = value; }
    public float                QComp                   { get => _ptr->qcompress;                   set => _ptr->qcompress = value; }
    public int                  QDiff                   { get => _ptr->max_qdiff;                   set => _ptr->max_qdiff = value; }
    public int                  QMin                    { get => _ptr->qmin;                        set => _ptr->qmin = value; }
    public int                  QMax                    { get => _ptr->qmax;                        set => _ptr->qmax = value; }

    public DCTAlgo              DCTAlgo                 { get => _ptr->dct_algo;                    set => _ptr->dct_algo = value; }
        
    public float                DarkMask                { get => _ptr->dark_masking;                set => _ptr->dark_masking = value; }
    public float                LumiMask                { get => _ptr->lumi_masking;                set => _ptr->lumi_masking = value; }
    public float                PMask                   { get => _ptr->p_masking;                   set => _ptr->p_masking = value; }
    public float                TCPLXMask               { get => _ptr->temporal_cplx_masking;       set => _ptr->temporal_cplx_masking = value; }
    public float                SCPLXMask               { get => _ptr->spatial_cplx_masking;        set => _ptr->spatial_cplx_masking = value; }

    public AVRational           SampleAspectRatio       { get => _ptr->sample_aspect_ratio;         set => _ptr->sample_aspect_ratio = value; } // Aspect
    public int                  DiaSize                 { get => _ptr->dia_size;                    set => _ptr->dia_size = value; }
    public int                  PreDiaSize              { get => _ptr->pre_dia_size;                set => _ptr->pre_dia_size = value; }
    public int                  SubPelQuality           { get => _ptr->me_subpel_quality;           set => _ptr->me_subpel_quality = value; }
    public int                  MeRange                 { get => _ptr->me_range;                    set => _ptr->me_range = value; }
    public int                  LastPredictor           { get => _ptr->last_predictor_count;        set => _ptr->last_predictor_count = value; }
    public MBDecision 
                                MBDecision              { get => _ptr->mb_decision;                 set => _ptr->mb_decision = value; }
    public int                  RCInitOccupancy         { get => _ptr->rc_initial_buffer_occupancy; set => _ptr->rc_initial_buffer_occupancy = value; }
    public int                  IntraDCPrecision        { get => _ptr->intra_dc_precision;          set => _ptr->intra_dc_precision = value; }
    public int                  NSEEWeith               { get => _ptr->nsse_weight;                 set => _ptr->nsse_weight = value; }

    public CompareFunction      MECmpFunc               { get => _ptr->me_cmp;                      set => _ptr->me_cmp = value; }
    public CompareFunction      MBCmpFunc               { get => _ptr->mb_cmp;                      set => _ptr->mb_cmp = value; }
    public CompareFunction      ILDCTCmpFunc            { get => _ptr->ildct_cmp;                   set => _ptr->ildct_cmp = value; }
    public CompareFunction      PreCmpFunc              { get => _ptr->me_pre_cmp;                  set => _ptr->me_pre_cmp = value; }
    public CompareFunction      SubCmpFunc              { get => _ptr->me_sub_cmp;                  set => _ptr->me_sub_cmp = value; }

    public int                  MBLMin                  { get => _ptr->mb_lmin;                     set => _ptr->mb_lmin = value; }
    public int                  MBLMax                  { get => _ptr->mb_lmax;                     set => _ptr->mb_lmax = value; }

    public int                  BidirRefine             { get => _ptr->bidir_refine;                set => _ptr->bidir_refine = value; }
    public int                  KeyIntMin               { get => _ptr->keyint_min;                  set => _ptr->keyint_min = value; }
    public int                  RefFrames               { get => _ptr->refs;                        set => _ptr->refs = value; }
    public int                  MV0Threshold            { get => _ptr->mv0_threshold;               set => _ptr->mv0_threshold = value; }

    public float                RCMinVBVUse             { get => _ptr->rc_min_vbv_overflow_use;     set => _ptr->rc_min_vbv_overflow_use = value; }
    public float                RCMaxVBVUse             { get => _ptr->rc_max_available_vbv_use;    set => _ptr->rc_max_available_vbv_use = value; }
    public int                  Slices                  { get => _ptr->slices;                      set => _ptr->slices = value; }
    #endregion

    public FFmpegClass          AVClass                 => FFmpegClass.Get(_ptr, EV)!;
    //public FFmpegClass?         AVClassPrivate          => FFmpegClass.Get(_ptr->priv_data, EV);
    public ThreadTypeFlags      ActiveThreadType        => _ptr->active_thread_type;
    public int                  Delay                   => _ptr->delay;
    public int                  VideoDelay              => _ptr->has_b_frames;
    public long                 FrameNumber             => _ptr->frame_num;
    public int                  BitsPerCodedSample      => _ptr->bits_per_coded_sample;
    public int                  FrameSize               => _ptr->frame_size;

    public HWFramesContextBase? HWFramesContext         { get => _ptr->hw_frames_ctx == null ? null : new HWFramesContextView(_ptr->hw_frames_ctx); set { if (_ptr->hw_frames_ctx != null || value == null) return; _ptr->hw_frames_ctx = value.RefRaw(); } }
    public VideoEncoderSpec     CodecSpec               { get; }

    public VideoEncoder(VideoEncoderSpec codec) : base(codec) { CodecSpec = codec; }

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

    public FFmpegResult SendFrame(VideoFrameBase frame)
        => new(avcodec_send_frame(_ptr, frame));
}
