namespace Flyleaf.FFmpeg.Codec;

public unsafe class CodecParserBase
{
    // TBR: RW? (AVSD Parser?)
    public ParserFlags  Flags               { get => (ParserFlags)_ptr->flags; set => _ptr->flags = (int)value; } // Related to AVStreamParseType (need_parsing)
    
    public long         Dts                 => _ptr->dts;
    public long         Pts                 => _ptr->pts;
    public long         Pos                 => _ptr->pos;
    public long         Offset              => _ptr->offset;
    public long         FrameOffset         => _ptr->frame_offset;

    public long         LastDts             => _ptr->last_dts;
    public long         LastPts             => _ptr->last_pts;
    public long         LastPos             => _ptr->last_pos;

    public int          CurFrameStartIndex  => _ptr->cur_frame_start_index;

    public ref readonly Array4<long>
                        CurFrameDts         => ref _ptr->cur_frame_dts;
    public ref readonly Array4<long>
                        CurFramePts         => ref _ptr->cur_frame_pts;
    public ref readonly Array4<long>
                        CurFramePos         => ref _ptr->cur_frame_pos;
    public ref readonly Array4<long>
                        CurFrameOffset      => ref _ptr->cur_frame_offset;
    public ref readonly Array4<long>
                        CurFrameEnd         => ref _ptr->cur_frame_end;
    
    public long         CurOffset           => _ptr->cur_offset;
    public long         NextFrameOffset     => _ptr->next_frame_offset;
    
    public int          FetchTimestamp      => _ptr->fetch_timestamp;
    public int          DtsRefDtsDelta      => _ptr->dts_ref_dts_delta;
    public int          PtsDtsDelta         => _ptr->pts_dts_delta;
    public int          DtsSyncPoint        => _ptr->dts_sync_point;
    
    public int          Duration            => _ptr->duration;
    public int          KeyFrame            => _ptr->key_frame;
    public int          Format              => _ptr->format;

    // V Only?*
    public int          Width               => _ptr->width;
    public int          Height              => _ptr->height;
    public int          CodedWidth          => _ptr->coded_width;
    public int          CodedHeight         => _ptr->coded_height;
    public AVFieldOrder FieldOrder          => _ptr->field_order;
    public AVPictureType
                        PictType            => (AVPictureType)_ptr->pict_type; // AVPictureType?
    public int          RepeatPict          => _ptr->repeat_pict;
    public int          OutputPictureNumber => _ptr->output_picture_number;
    public AVPictureStructure
                        PictureStructure    => _ptr->picture_structure;

    public bool         Disposed            => _ptr == null;

    public readonly AVCodecParserContext* _ptr;

    public static implicit operator AVCodecParserContext*(CodecParserBase parser)
        => parser._ptr;

    protected CodecParserBase(AVCodecParserContext* ptr)
        => _ptr = ptr;

    public CodecParserBase(AVCodecID codecId)
        => _ptr = av_parser_init(codecId);

    public int Parse(AVCodecContext* decoder, byte* srcData, int srcSize, byte** dstData, int* dstSize, long dts = NoTs, long pts = NoTs, long pos = 0)
        => av_parser_parse2(_ptr, decoder, dstData, dstSize, srcData, srcSize, pts, dts, pos);
}

public unsafe class CodecParser(AVCodecID codecId) : CodecParserBase(codecId), IDisposable
{
    #region Disposal
    ~CodecParser()
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

    static System.Reflection.FieldInfo ptrField = typeof(CodecParser).GetField(nameof(_ptr))!;
    void Free()
    {
        av_parser_close(_ptr);
        ptrField.SetValue(this, null);
    }
    #endregion
}

public unsafe class CodecParserView(AVCodecParserContext* ptr) : CodecParserBase(ptr) { }
