namespace Flyleaf.FFmpeg.Format.Demux;

/// <summary>
/// Read-only MediaStream View for both Muxer and Demuxer
/// </summary>
public unsafe abstract class MediaStream
{
    public int                  Id                  => _ptr->id;
    public DispositionFlags     Disposition         => _ptr->disposition;
    public long                 Duration            => _ptr->duration;
    public long                 Frames              => _ptr->nb_frames;
    public Dictionary<string, string>?
                                Metadata            => AVDictToDict(_ptr->metadata); //{ get; private set; }
    public string?              MetadataGet(string key, DictReadFlags flags = DictReadFlags.None)
                                                    { var val = av_dict_get(_ptr->metadata, key, null, flags); return val != null ? GetString(val->value) : null; }
    public long                 StartTime           => _ptr->start_time;
    public AVRational           Timebase            => _ptr->time_base;

    // RW always
    public StreamEventFlags     EventFlags          { get => _ptr->event_flags;               set => _ptr->event_flags = value; } // we could only unset but not set flags?

    // RO always (maybe not for custom demuxer*)
    public int                  Index               => _ptr->index;
    public int                  PtsWrapBits         => _ptr->pts_wrap_bits;

    // == CodecParams ==
    public long                 BitRate             => _codecpar->bit_rate;
    public int                  BitsPerCodedSample  => _codecpar->bits_per_coded_sample;
    public int                  BitsPerRawSample    => _codecpar->bits_per_raw_sample;

    public byte*                ExtraData           => _codecpar->extradata;
    public int                  ExtraDataSize       => _codecpar->extradata_size;

    public void ExtraDataCopyTo(byte** dstPtr, int* dstSize)
        => ExtraDataCopy(_codecpar->extradata, _codecpar->extradata_size, dstPtr, dstSize);

    #region IndexEntries
    public int AddEntry(long pos, long timestamp, int size, int distance, IndexFlags flags)
        => av_add_index_entry(this, pos, timestamp, size, distance, flags);

    public int GetEntriesCount()
        => avformat_index_get_entries_count(this);

    public AVIndexEntry* GetEntry(int index)
        => avformat_index_get_entry(this, index);

    public AVIndexEntry* GetEntry(long timestamp, SeekFlags flags)
        => avformat_index_get_entry_from_timestamp(this, timestamp, flags);

    public int SearchTimestamp(long timestamp, SeekFlags flags)
        => av_index_search_timestamp(this, timestamp, flags);
    #endregion

    #region SideData RO
    public AVPacketSideData*    SideData            => _codecpar->coded_side_data;
    public int                  SideDataCount       => _codecpar->nb_coded_side_data;

    public AVPacketSideData* SideDataGet(AVPacketSideDataType type)
        => av_packet_side_data_get(_codecpar->coded_side_data, _codecpar->nb_coded_side_data, type);

    public void SideDataCopyTo(AVPacketSideData** dstPtr, int* dstCount)
        => SideDataCopy(_codecpar->coded_side_data,_codecpar->nb_coded_side_data, dstPtr, dstCount);
    #endregion

    // should be readonly (set in the constructor*)
    public CodecDescriptor?     CodecDescriptor     => FindCodecDescriptor(CodecId); // CodecId might change/update from decoders
    public AVCodecID            CodecId             => _codecpar->codec_id;
    public AVMediaType          CodecType           => _codecpar->codec_type; // hide (except unknown/generic)?
    public uint                 CodecTag            => _codecpar->codec_tag; // ??
    public CodecParserView?     CodecParser         { get { var parser = av_stream_get_parser(_ptr); return parser == null ? null : new(parser); } }

    // helpers
    //public string               CodecName           => avcodec_get_name(CodecId);
    //public string               CodecTagName        => GetFourCCString(CodecTag);
    public bool                 Enabled             => _ptr->discard != AVDiscard.All; // TBR: Demux only but here is just RO (can be used from Muxer)

    public Muxer?               Muxer               { get; private set; }
    public Demuxer?             Demuxer             { get; private set; }

    public ReadOnlyCollection<MediaProgram> Programs    = null!; // HLS, DASH, MPEGTS
    public ReadOnlyCollection<StreamGroup>  StreamGroups= null!;
    internal List<MediaProgram>             programs    = [];
    internal List<StreamGroup>              streamGroups= [];

    public readonly AVStream* _ptr;
    public readonly AVCodecParameters* _codecpar;

    public static implicit operator AVStream*(MediaStream stream)
        => stream._ptr;

    public static implicit operator AVCodecParameters*(MediaStream stream)
        => stream._codecpar;

    protected MediaStream(FormatContext fmt, AVStream* stream)
    {
        if (fmt is Muxer muxer)
            Muxer = muxer;
        else
            Demuxer = (Demuxer) fmt;

        _ptr        = stream;
        _codecpar   = _ptr->codecpar;
        Programs    = new(programs);
        StreamGroups= new(streamGroups);
        //CodecDescriptor = FindCodecDescriptor(CodecId);
    }

    public static MediaStream Get(FormatContext fmt, AVStream* stream) => stream->codecpar->codec_type switch 
    {
        AVMediaType.Audio       => new AudioStream      (fmt, stream),
        AVMediaType.Video       => new VideoStream      (fmt, stream),
        AVMediaType.Subtitle    => new SubtitleStream   (fmt, stream),
        AVMediaType.Data        => new DataStream       (fmt, stream),
        AVMediaType.Attachment  => new AttachmentStream (fmt, stream),
        _                       => new UnknownStream    (fmt, stream),
    };

    public string GetDump()
    {
        var metadata = Metadata;
        string dump = $"[Stream #{Index:D2}] {CodecType}";
        if (this is not VideoStream && metadata != null && metadata.TryGetValue("language", out string? lang))
            dump += $" ({lang})";
        
        if (StartTime != NoTs || Duration != NoTs)
        {
            dump += "\r\n\t[Time	 ] ";
            dump += StartTime != NoTs ? $"{McsToTime(av_rescale_q(StartTime, Timebase, TIME_BASE_Q))} ({StartTime})" : "-";
            dump += " / ";
            dump += Duration != NoTs ? $"{McsToTime(av_rescale_q(Duration, Timebase, TIME_BASE_Q))} ({Duration})": "-";
            dump += $" (tb: {Timebase})";
        }

        var codecDescriptor = CodecDescriptor;

        if (codecDescriptor != null)
            dump+= $"\r\n\t[Codec   ] {codecDescriptor.Name} | {GetProfile(codecDescriptor.Profiles, _codecpar->profile).Name}";
        else
            dump+= $"\r\n\t[Codec   ] {CodecId}";

        if (CodecTag != 0)
            dump += $" ({GetFourCCString(CodecTag)} / 0x{CodecTag:X4})";

        if (BitRate > 0)
            dump += $", {(int)(BitRate / 1000)} kb/s";

        if (Disposition != DispositionFlags.None)
            dump += $" - ({GetFlagsAsString(Disposition)})";

        if (this is AudioStream audio)
            dump += $"\r\n\t[Format  ] {audio.SampleRate} Hz, {audio.ChannelLayout.GetName()}, {audio.SampleFormat.GetName()}";
        else if (this is VideoStream video)
            dump += $"\r\n\t[Format  ] {video.PixelFormat} ({video.ColorSpace}, {video.ColorRange}, {video.ColorPrimaries}, {video.ColorTransfer}, {video.ChromaLocation}, {video.FieldOrder}), {video.Width}x{video.Height} @ {DoubleToTimeMini(video.FrameRate.ToDouble())} fps [SAR1: {video.SampleAspectRatio} SAR2: {video.SampleAspectRatio} DAR: {video.GetDisplayAspectRatio()}]";

        if (metadata != null)
            dump += $"\r\n{DumpMetadata(metadata, "language")}";
        
        return dump;
    }
}
