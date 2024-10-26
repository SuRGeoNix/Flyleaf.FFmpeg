namespace Flyleaf.FFmpeg.Format.Mux;

public unsafe abstract class MediaStreamMux
{
    public int                  Id                  { get => _ptr->id;                          set => _ptr->id = value; }
    public DispositionFlags     Disposition         { get => _ptr->disposition;                 set => _ptr->disposition = value; }
    public long                 Duration            { get => _ptr->duration;                    set => _ptr->duration = value; }
    public long                 Frames              { get => _ptr->nb_frames;                   set => _ptr->nb_frames = value; }
    public Dictionary<string, string>?
                                Metadata            { get => AVDictToDict(_ptr->metadata);      set => AVDictReplaceFromDict(value, &_ptr->metadata); } // "movflags" = "use_metadata_tags" auto header opts?
    public string?              MetadataGet(string key, DictReadFlags flags = DictReadFlags.None)
                                                    { var val = av_dict_get(_ptr->metadata, key, null, flags); return val != null ? GetString(val->value) : null; }
    public int                  MetadataSet(string key, string value, DictWriteFlags flags = DictWriteFlags.None)
                                                    => av_dict_set(&_ptr->metadata, key, value, flags);

    public long                 StartTime           { get => _ptr->start_time;                  set => _ptr->start_time = value; }
    public AVRational           Timebase            { get => _ptr->time_base;                   set => _ptr->time_base = value; }

    // RW always
    public StreamEventFlags     EventFlags          { get => _ptr->event_flags;                 set => _ptr->event_flags = value; } // we could only unset but not set flags?

    // RO always (maybe not for custom demuxer*)
    public int                  Index               => _ptr->index;
    public int                  PtsWrapBits         => _ptr->pts_wrap_bits;

    // == CodecParams ==
    public long                 BitRate             { get => _codecpar->bit_rate;               set => _codecpar->bit_rate = value; }
    public int                  BitsPerCodedSample  { get => _codecpar->bits_per_coded_sample;  set => _codecpar->bits_per_coded_sample = value; }
    public int                  BitsPerRawSample    { get => _codecpar->bits_per_raw_sample;    set => _codecpar->bits_per_raw_sample = value; }

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

    // should be readonly (set in the constructor*)
    public CodecDescriptor?     CodecDescriptor     { get; }
    public AVCodecID            CodecId             => _codecpar->codec_id;
    public AVMediaType          CodecType           => _codecpar->codec_type;
    public uint                 CodecTag            { get => _codecpar->codec_tag;              set => _codecpar->codec_tag = value; } // ??

    // helpers
    //public string               CodecTagName        => GetFourCCString(CodecTag);

    public Muxer                Muxer               { get; private set; }

    public readonly AVStream* _ptr;
    public readonly AVCodecParameters* _codecpar;

    protected MediaStreamMux(Muxer muxer, AVMediaType type, AVCodecID codecId)
    {
        Muxer                       = muxer;
        _ptr                        = muxer.NewStream(type);
        _ptr->codecpar->codec_id    = codecId;
        _codecpar                   = _ptr->codecpar;
        CodecDescriptor             = FindCodecDescriptor(codecId);
    }

    public static implicit operator AVStream*(MediaStreamMux stream)
        => stream._ptr;

    public static implicit operator AVCodecParameters*(MediaStreamMux stream)
        => stream._codecpar;
}
