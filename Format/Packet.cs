namespace Flyleaf.FFmpeg.Format;

/* TODO: FFmpeg wrapper performance cleanup
    - Remove finalizers
    - Avoid ref/fixed on _ptr fields
    - Use a local ptr in constructors before assigning _ptr
    - Keep _ptr mutable and avoid readonly/reflection workarounds
    - Force constructor AggressiveInlining (consider also Dispose) so short-lived wrappers can be eliminated completely
    - Prefer NativeMemory.AllocZeroed/Free where ownership allows
      (~10-13% faster than FFmpeg av_packet/frame_alloc/free in benchmarks)
*/

public abstract class PacketFrameSubs : IDisposable
{
    public abstract long    Pts             { get; set; }

    public abstract string  GetDump(AVRational timebase);
    public abstract void    Dispose();
}

public abstract class PacketFrame : PacketFrameSubs
{
    public abstract long    Duration        { get; set; }

    public abstract void    UnRef();
}

public unsafe sealed class Packet : PacketFrame
{
    public byte*            Data            => _ptr->data;
    public int              Size            => _ptr->size;

    public long             Dts             { get => _ptr->dts;             set => _ptr->dts = value; }
    public override long    Pts             { get => _ptr->pts;             set => _ptr->pts = value; }
    public override long    Duration        { get => _ptr->duration;        set => _ptr->duration = value; }

    public PktFlags         Flags           { get => _ptr->flags;           set => _ptr->flags = value; }
    public long             Pos             { get => _ptr->pos;             set => _ptr->pos = value; }
    
    public int              StreamIndex     { get => _ptr->stream_index;    set => _ptr->stream_index = value; }

    public bool             Disposed        => _ptr == null;
    public AVPacket* _ptr;

    public static implicit operator AVPacket*(Packet pkt)
        => pkt._ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Packet()
    {
        var ptr = (AVPacket*)NativeMemory.AllocZeroed((nuint)sizeof(AVPacket));
        ptr->pts           = NoTs;
        ptr->dts           = NoTs;
        ptr->pos           = -1;
        ptr->time_base.Den = 1;

        _ptr = ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Dispose()
    { 
        var ptr = _ptr;
        if (ptr == null)
            return;

        _ptr = null;

        av_packet_unref(ptr);
        NativeMemory.Free(ptr);
    }

    internal Packet(AVPacket* ptr)
        => _ptr = ptr;

    public Packet(int dataSize) : this()
        => new FFmpegResult(av_new_packet(_ptr, dataSize)).ThrowOnFailure();

    public Packet(byte* data, int dataSize) : this()
        => new FFmpegResult(av_packet_from_data(_ptr, data, dataSize)).ThrowOnFailure();

    public Span<byte> DataAsSpan()
        => new(_ptr->data, _ptr->size);

    public int CopyProperties(AVPacket* pkt)
        => av_packet_copy_props(pkt, _ptr);

    public int CopyProperties(Packet pkt)
         => av_packet_copy_props(pkt._ptr, _ptr);

    public void RescaleTimestamp(AVRational source, AVRational dest) // TBR (stream, another packet, anything with timebase?) + rescale options?
        => av_packet_rescale_ts(_ptr, source, dest);

    public void RescaleTimestamp(AVRational source, AVRational dest, int streamIndex)
    {
        av_packet_rescale_ts(_ptr, source, dest);
        StreamIndex = streamIndex;
    }

    public void Shrink(int size)
        => av_shrink_packet(_ptr, size);

    public int Grow(int growBy)
        => av_grow_packet(_ptr, growBy);

    public int MakeWriteable()
        => av_packet_make_writable(_ptr);

    public int Ref(AVPacket* pkt)
        => av_packet_ref(pkt, _ptr);

    public int Ref(Packet pkt)
        => av_packet_ref(pkt._ptr, _ptr);

    public Packet Ref()
    {
        Packet packet = new();
        Ref(packet);
        return packet;
    }

    public override void UnRef()
        => av_packet_unref(_ptr);

    public void MoveRef(AVPacket* pkt)
        => av_packet_move_ref(pkt, _ptr);

    public void MoveRef(Packet pkt)
        => MoveRef(pkt._ptr);

    public AVPacket* CloneRaw()
        => av_packet_clone(_ptr);

    public Packet? Clone()
    {
        Packet packet = new();
        if (av_packet_ref(packet._ptr, _ptr) < 0)
        {
            packet.Dispose();
            return null;
        }

        return packet;
    }

    public void Dump(AVStream* stream, bool payload = false, LogLevel logLevel = LogLevel.Debug)
        => av_pkt_dump_log2(null, logLevel, _ptr, payload ? 1 : 0, stream);

    public string GetDump(AVRational timebase, char mediaType)
        => $"[{mediaType}#{_ptr->stream_index:D2}] {GetDump(timebase)}";

    public override string GetDump(AVRational timebase)
    {
        string? sideData = null;

        if (_ptr->side_data_elems > 0)
        {
            for (int i = 0; i < _ptr->side_data_elems - 1; i++)
                sideData += av_packet_side_data_name(_ptr->side_data[i].type) + "|";

            sideData += av_packet_side_data_name(_ptr->side_data[_ptr->side_data_elems - 1].type);
        }

        string? flags = _ptr->flags != 0 ? GetFlagsAsString(_ptr->flags, "|") : null;
        
        string dts, Dts, pts, Pts, dur, Dur;

        if (_ptr->dts != NoTs)
        {
            dts = McsToTimeMini(av_rescale_q(_ptr->dts, timebase, TIME_BASE_Q));
            Dts = _ptr->dts.ToString();
        }
        else
            dts = Dts = "-";

        if (_ptr->pts != NoTs)
        {
            pts = McsToTimeMini(av_rescale_q(_ptr->pts, timebase, TIME_BASE_Q));
            Pts = _ptr->pts.ToString();
        }
        else
            pts = Pts = "-";

        if (_ptr->duration > 0)
        {
            dur = McsToTimeMini(av_rescale_q(_ptr->duration, timebase, TIME_BASE_Q));
            Dur = _ptr->duration.ToString();
        }
        else
            dur = Dur = "-";
        
        return $"dts: {dts + " (" + Dts + ")",-25},pts: {pts + " (" + Pts + ")",-25},dur: {dur + " (" + Dur + ")",-20},pos: {_ptr->pos,-12},size: {_ptr->size,-7}{(flags != null ? ",flags: [" + flags + "]" : "")}{(sideData != null ? ",side: [" + sideData + "]" : "")}";
    }

    #region SideData
    public AVPacketSideData*    SideData        => _ptr->side_data;
    public int                  SideDataCount   => _ptr->side_data_elems;

    public AVPacketSideData* SideDataGet(AVPacketSideDataType type)
        => av_packet_side_data_get(_ptr->side_data, _ptr->side_data_elems, type);

    public void SideDataCopyTo(AVPacketSideData** dstPtr, int* dstCount)
        => SideDataCopy(_ptr->side_data, _ptr->side_data_elems, dstPtr, dstCount);
    
    public AVPacketSideData* SideDataNew(AVPacketSideDataType type, nuint size)
        => av_packet_side_data_new(&_ptr->side_data, &_ptr->side_data_elems, type, size, 0);

    public AVPacketSideData* SideDataAdd(AVPacketSideDataType type, byte* data, nuint dataSize) // TBR: *byte / must be malloc*
        => av_packet_side_data_add(&_ptr->side_data, &_ptr->side_data_elems, type, data, dataSize, 0);

    public void SideDataRemove(AVPacketSideDataType type)
        => av_packet_side_data_remove(_ptr->side_data, &_ptr->side_data_elems, type);

    public void SideDataFree()
        => av_packet_side_data_free(&_ptr->side_data, &_ptr->side_data_elems);
    #endregion
}
