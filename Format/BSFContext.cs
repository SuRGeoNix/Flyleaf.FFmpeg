namespace Flyleaf.FFmpeg.Format;

// TBR: AVBSFList anyone using this?

public unsafe class BSFContext : IDisposable
{
    public FFmpegClass      AVClass             => FFmpegClass.Get(_ptr, D)!;
    public FFmpegClass?     AVClassPrivate      => FFmpegClass.Get(_ptr->priv_data);
    public BSFSpec          BSFSpec             { get; }

    public BSFParameters    ParametersIn        { get; }
    public BSFParameters    ParametersOut       { get; }

    public bool             Disposed            => _ptr == null;

    public readonly AVBSFContext* _ptr;

    public BSFContext(BSFSpec filter)
    {
        fixed(AVBSFContext** ptrPtr = &_ptr)
            new FFmpegResult(av_bsf_alloc(filter, ptrPtr)).ThrowOnFailure();

        BSFSpec         = filter;
        ParametersIn    = new(_ptr, _ptr->par_in);
        ParametersOut   = new(_ptr, _ptr->par_out);
    }

    public FFmpegResult Init()
        => new(av_bsf_init(_ptr));

    public void Flush()
        => av_bsf_flush(_ptr);

    public FFmpegResult SendPacket(AVPacket* packet)
        => new(av_bsf_send_packet(_ptr, packet));

    public FFmpegResult SendPacket(Packet packet)
        => new(av_bsf_send_packet(_ptr, packet._ptr));

    public FFmpegResult RecvPacket(AVPacket* packet)
        => new(av_bsf_receive_packet(_ptr, packet));

    public FFmpegResult RecvPacket(Packet packet)
        => new(av_bsf_receive_packet(_ptr, packet._ptr));

    public FFmpegResult Drain()
        => new(av_bsf_send_packet(_ptr, null));

    #region Disposal
    ~BSFContext()
    {
        if (!Disposed)
            Free();
    }

    public void Dispose()
    { 
        if (!Disposed)
        {
            Free();
            GC.SuppressFinalize(this);
        }
    }

    void Free()
    {
        fixed(AVBSFContext** ptrPtr = &_ptr)
            av_bsf_free(ptrPtr);
    }
    #endregion
}

public unsafe class BSFParameters
{
    public AVRational           Timebase            { get => _ptr->time_base_in;                set => _ptr->time_base_in = value; }
    public AVCodecID            CodecId             { get => _codecpar->codec_id;               set => _codecpar->codec_id = value; }
    public AVMediaType          CodecType           { get => _codecpar->codec_type;             set => _codecpar->codec_type = value; }
    public uint                 CodecTag            { get => _codecpar->codec_tag;              set => _codecpar->codec_tag = value; } // ??
    public int                  Format              { get => _codecpar->format;                 set => _codecpar->format = value; }
    public long                 BitRate             { get => _codecpar->bit_rate;               set => _codecpar->bit_rate = value; }
    public int                  BitsPerCodedSample  { get => _codecpar->bits_per_coded_sample;  set => _codecpar->bits_per_coded_sample = value; }
    public int                  BitsPerRawSample    { get => _codecpar->bits_per_raw_sample;    set => _codecpar->bits_per_raw_sample = value; }

    public int                  Level               { get => _codecpar->level;                  set => _codecpar->level = value; }
    //public CodecProfile         CodecProfile        { get => CodecDescriptor == null ? PROFILE_UNKNOWN : GetProfile(CodecDescriptor.Profiles, _codecpar->profile); set => _codecpar->profile = value.Profile; }

    public int                  BlockAlign          { get => _codecpar->block_align;            set => _codecpar->block_align = value; }
    public AVChannelLayout      ChannelLayout       { get => _codecpar->ch_layout;              set => _ = av_channel_layout_copy(&_codecpar->ch_layout, &value); } // frees prev
    public int                  FrameSize           { get => _codecpar->frame_size;             set => _codecpar->frame_size = value; }
    public int                  InitPad             { get => _codecpar->initial_padding;        set => _codecpar->initial_padding = value; }
    public AVSampleFormat       SampleFormat        { get => (AVSampleFormat)_codecpar->format; set => _codecpar->format = (int)value; }
    public int                  SampleRate          { get => _codecpar->sample_rate;            set => _codecpar->sample_rate = value; }
    public int                  SeekPreRoll         { get => _codecpar->seek_preroll;           set => _codecpar->seek_preroll = value; }
    public int                  TrailPad            { get => _codecpar->trailing_padding;       set => _codecpar->trailing_padding= value; }

    public AVChromaLocation     ChromaLocation      { get => _codecpar->chroma_location;        set => _codecpar->chroma_location = value; }
    public AVColorPrimaries     ColorPrimaries      { get => _codecpar->color_primaries;        set => _codecpar->color_primaries = value; }
    public AVColorRange         ColorRange          { get => _codecpar->color_range;            set => _codecpar->color_range = value; }
    public AVColorSpace         ColorSpace          { get => _codecpar->color_space;            set => _codecpar->color_space = value; }
    public AVColorTransferCharacteristic
                                ColorTransfer       { get => _codecpar->color_trc;              set => _codecpar->color_trc = value; }
    public int                  VideoDelay          { get => _codecpar->video_delay;            set => _codecpar->video_delay = value; }
    public AVFieldOrder         FieldOrder          { get => _codecpar->field_order;            set => _codecpar->field_order = value; }
    public int                  Height              { get => _codecpar->height;                 set => _codecpar->height = value; }
    public AVPixelFormat        PixelFormat         { get => (AVPixelFormat)_codecpar->format;  set => _codecpar->format = (int)value; }
    public AVRational           SampleAspectRatio   { get => _codecpar->sample_aspect_ratio;    set => _codecpar->sample_aspect_ratio = value;}
    public int                  Width               { get => _codecpar->width;                  set => _codecpar->width = value; }
    public AVRational           FrameRate           { get => _codecpar->framerate;              set => _codecpar->framerate = value; }

    public byte*                ExtraData           { get => _codecpar->extradata;              set => _codecpar->extradata = value; }
    public int                  ExtraDataSize       { get => _codecpar->extradata_size;         set => _codecpar->extradata_size = value; }

    public void ExtraDataCopyTo(byte** dstPtr, int* dstSize)
        => ExtraDataCopy(_codecpar->extradata, _codecpar->extradata_size, dstPtr, dstSize);

    #region SideData
    public AVPacketSideData*    SideData            => _codecpar->coded_side_data;
    public int                  SideDataCount       => _codecpar->nb_coded_side_data;

    public AVPacketSideData* SideDataGet(AVPacketSideDataType type)
        => av_packet_side_data_get(_codecpar->coded_side_data, _codecpar->nb_coded_side_data, type);

    public void SideDataCopyTo(AVPacketSideData** dstPtr, int* dstCount)
        => SideDataCopy(_codecpar->coded_side_data,_codecpar->nb_coded_side_data, dstPtr, dstCount);

    public AVPacketSideData* SideDataNew(AVPacketSideDataType type, nuint size)
        => av_packet_side_data_new(&_codecpar->coded_side_data, &_codecpar->nb_coded_side_data, type, size, 0);

    public AVPacketSideData* SideDataAdd(AVPacketSideDataType type, byte* data, nuint dataSize) // TBR: *byte / must be malloc*
        => av_packet_side_data_add(&_codecpar->coded_side_data, &_codecpar->nb_coded_side_data, type, data, dataSize, 0);

    public void SideDataRemove(AVPacketSideDataType type)
        => av_packet_side_data_remove(_codecpar->coded_side_data, &_codecpar->nb_coded_side_data, type);

    public void SideDataFree()
        => av_packet_side_data_free(&_codecpar->coded_side_data, &_codecpar->nb_coded_side_data);
    #endregion

    AVBSFContext*       _ptr;
    AVCodecParameters*  _codecpar;

    internal BSFParameters(AVBSFContext* ptr, AVCodecParameters* codecpar)
    {
        _ptr        =  ptr;
        _codecpar   = codecpar;
    }
}