namespace Flyleaf.FFmpeg.Codec;

public unsafe abstract class Frame : PacketFrame
{
    // allocate/owned, dispose/unref/free
    // data, linesize, extended_data, buf/extended_buf
    // frameSideData

    public override long                Pts                     { get => _ptr->pts;                    set => _ptr->pts = value; }
    public long                         PktDts                  { get => _ptr->pkt_dts;                set => _ptr->pkt_dts = value; }
    public long                         PtsBest                 { get => _ptr->best_effort_timestamp;  set => _ptr->best_effort_timestamp = value; }
    public override long                Duration                { get => _ptr->duration;               set => _ptr->duration = value; }

    public DecodeErrorFlags             DecodeErrorFlags        { get => _ptr->decode_error_flags;     set => _ptr->decode_error_flags = value; }
    public int                          RepeatPict              { get => _ptr->repeat_pict;            set => _ptr->repeat_pict = value; } // V only?
    public Dictionary<string, string>?  Metadata                { get => AVDictToDict(_ptr->metadata); set => AVDictReplaceFromDict(value, &_ptr->metadata); }
    public string?                      MetadataGet(string key, DictReadFlags flags = DictReadFlags.None)
                                                                { var val = av_dict_get(_ptr->metadata, key, null, flags); return val != null ? GetString(val->value) : null; }
    public int                          MetadataSet(string key, string value, DictWriteFlags flags = DictWriteFlags.None)
                                                                => av_dict_set(ref _ptr->metadata, key, value, flags);
    
    public FrameFlags                   Flags                   { get => _ptr->flags;                  set => _ptr->flags = value; }
    public bool                         IsWritable              => av_frame_is_writable(_ptr) != 0;

    public AVRational                   Timebase                { get => _ptr->time_base;              set => _ptr->time_base = value; } // V only (filter only?)

    // NOTE: for Video those should be always 4 (Extended only for Audio and linesize[0] only for Audio - for planar all must have same linesize)
    public ref Array8<nint>             BufferRefs              => ref _ptr->buf;
    public ref Array8<nint>             Data                    => ref _ptr->data;
    public ref Array8<int>              Linesize                => ref _ptr->linesize;
    
    public bool                         Disposed                => _ptr == null;
    public AVFrame* _ptr;

    public static implicit operator AVFrame*(Frame frame)
        => frame._ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Frame()
    {
        var ptr = (AVFrame*)NativeMemory.AllocZeroed((nuint)sizeof(AVFrame));
        ptr->pts                   = NoTs;
        ptr->pkt_dts               = NoTs;
        ptr->best_effort_timestamp = NoTs;
        ptr->time_base.Den         = 1;
        ptr->format                = -1;
        ptr->extended_data         = (byte**)&ptr->data;

        _ptr = ptr;
    }

    #pragma warning disable CA1816
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Dispose()
    {
        var ptr = _ptr;
        if (ptr == null)
            return;

        _ptr = null;

        av_frame_unref(ptr);
        NativeMemory.Free(ptr);
    }
    #pragma warning restore CA1816

    internal Frame(AVFrame* ptr)
        => _ptr = ptr;

    protected FFmpegResult InitBuffer(int align = 0)
        => new(av_frame_get_buffer(_ptr, align));

    public FFmpegResult MakeWritable()
        => new(av_frame_make_writable(_ptr));

    public void MoveRef(AVFrame* frame)
        => av_frame_move_ref(frame, this); // dst must be an owner?

    public AVFrame* CloneRaw()
        => av_frame_clone(this);

    public FFmpegResult CopyPropertiesTo(AVFrame* frame)
        => new(av_frame_copy_props(frame, this));

    public FFmpegResult Ref(AVFrame* frame)
        => new(av_frame_ref(frame, this));

    public override void UnRef()
        => av_frame_unref(_ptr);

    public string GetDump(AVRational timebase, int streamIndex, char mediaType)
        => $"[{mediaType}#{streamIndex:D2}] {GetDump(timebase)}";

    public override string GetDump(AVRational timebase)
    {
        string? sideData = null;

        if (_ptr->nb_side_data > 0)
        {
            for (int i = 0; i < _ptr->nb_side_data - 1; i++)
                sideData += _ptr->side_data[i]->type + "|";

            sideData += _ptr->side_data[_ptr->nb_side_data - 1]->type;
        }

        string? flags = _ptr->flags != 0 ? GetFlagsAsString(_ptr->flags, "|") : null;
        
        string dts, Dts, pts, Pts, dur, Dur;

        if (_ptr->pkt_dts != NoTs)
        {
            dts = McsToTimeMini(av_rescale_q(_ptr->pkt_dts, timebase, TIME_BASE_Q)); //DoubleToTime(pkt->dts * av_q2d(Stream.Timebase));
            Dts = _ptr->pkt_dts.ToString();
        }
        else
        {
            dts = Dts = "-";
        }

        if (_ptr->pts != NoTs)
        {
            pts = McsToTimeMini(av_rescale_q(_ptr->pts, timebase, TIME_BASE_Q)); // DoubleToTime(pkt->pts * av_q2d(Stream.Timebase));
            Pts = _ptr->pts.ToString();
        }
        else
        {
            pts = Pts = "-";
        }

        if (_ptr->duration > 0)
        {
            dur = McsToTimeMini(av_rescale_q(_ptr->duration, timebase, TIME_BASE_Q)); //DoubleToTime(pkt->duration * av_q2d(Stream.Timebase));
            Dur = _ptr->duration.ToString();
        }
        else
        {
            dur = Dur = "-";
        }
        
        return $"dts: {dts + " (" + Dts + ")",-25},pts: {pts + " (" + Pts + ")",-25},dur: {dur + " (" + Dur + ")",-20},picType: {_ptr->pict_type, -4}{(flags != null ? ",flags: [" + flags + "]" : "")}{(sideData != null ? ",side: [" + sideData + "]" : "")}";
    }

    #region Frame Side Data
    public AVFrameSideData**    SideData        => _ptr->side_data;
    public int                  SideDataCount   => _ptr->nb_side_data;

    public AVFrameSideData* SideDataGet(AVFrameSideDataType type)
        => av_frame_side_data_get_c(_ptr->side_data, _ptr->nb_side_data, type);

    public FFmpegResult SideDataCopyTo(AVFrameSideData*** dstPtr, int* dstCount, FrameSideDataFlags flags = FrameSideDataFlags.None)
        => SideDataCopy(_ptr->side_data, _ptr->nb_side_data, dstPtr, dstCount, flags);

    public AVFrameSideData* SideDataNew(AVFrameSideDataType type, nuint size, FrameSideDataFlags flags = FrameSideDataFlags.None)
        => av_frame_side_data_new(&_ptr->side_data, &_ptr->nb_side_data, type, size, (uint)flags);

    public AVFrameSideData* SideDataAdd(AVFrameSideDataType type, AVBufferRef** buffer, FrameSideDataFlags flags = FrameSideDataFlags.None)
        => av_frame_side_data_add(&_ptr->side_data, &_ptr->nb_side_data, type, buffer, (uint)flags);

    public void SideDataRemove(AVFrameSideDataType type)
        => av_frame_side_data_remove(&_ptr->side_data, &_ptr->nb_side_data, type);

    public void SideDataRemoveByProps(AVSideDataProps props)
        => av_frame_side_data_remove_by_props(&_ptr->side_data, &_ptr->nb_side_data, props);

    public void SideDataFree()
        => av_frame_side_data_free(&_ptr->side_data, &_ptr->nb_side_data);
    #endregion
}
