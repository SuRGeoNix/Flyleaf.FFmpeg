namespace Flyleaf.FFmpeg.Format;

// Even if PacketView is not required this might be helpful to allow custom packet implementations
public unsafe abstract class PacketBase
{
    public byte*        Data            => _ptr->data;
    public int          Size            => _ptr->size;

    public long         Dts             { get => _ptr->dts;             set => _ptr->dts = value; }
    public long         Pts             { get => _ptr->pts;             set => _ptr->pts = value; }
    public long         Duration        { get => _ptr->duration;        set => _ptr->duration = value; }

    public PktFlags     Flags           { get => _ptr->flags;           set => _ptr->flags = value; }
    public long         Pos             { get => _ptr->pos;             set => _ptr->pos = value; }
    
    public int          StreamIndex     { get => _ptr->stream_index;    set => _ptr->stream_index = value; }

    public bool         Disposed        => _ptr == null;
    public readonly AVPacket* _ptr;

    public static implicit operator AVPacket*(PacketBase pkt)
        => pkt._ptr;

    protected PacketBase()
        => _ptr = av_packet_alloc();

    protected PacketBase(AVPacket* ptr)
        => _ptr = ptr;

    public Span<byte> DataAsSpan()
        => new(_ptr->data, _ptr->size);

    public int CopyProperties(AVPacket* pkt)
        => av_packet_copy_props(pkt, _ptr);

    public int CopyProperties(PacketBase pkt)
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

    public void MakeWriteable()
        => av_packet_make_writable(_ptr);

    public int Ref(AVPacket* pkt)
        => av_packet_ref(pkt, _ptr);

    public int Ref(PacketBase pkt)
        => av_packet_ref(pkt._ptr, _ptr);

    public Packet Ref()
    {
        Packet packet = new();
        Ref(packet);
        return packet;
    }

    public void UnRef()
        => av_packet_unref(_ptr);

    public void MoveRef(AVPacket* pkt)
        => av_packet_move_ref(pkt, _ptr);

    public void MoveRef(PacketBase pkt)
        => MoveRef(pkt._ptr);

    public AVPacket* CloneRaw()
        => av_packet_clone(_ptr);

    public Packet Clone()
        => new(av_packet_clone(_ptr));

    public void Dump(AVStream* stream, bool payload = false)
        => av_pkt_dump_log2(null, 0, _ptr, payload ? 1 : 0, stream);

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

// View from existing owned packet based on ptr - no finalizer/disposal (TBR: if required)
public unsafe sealed class PacketView(AVPacket* ptr) : PacketBase(ptr) { }

// Owned Disposable Packet
public unsafe sealed class Packet : PacketBase, IDisposable
{
    public Packet() : base() { }

    public Packet(int dataSize) : base()
        => new FFmpegResult(av_new_packet(_ptr, dataSize)).ThrowOnFailure();

    public Packet(byte* data, int dataSize) : base()
        => new FFmpegResult(av_packet_from_data(_ptr, data, dataSize)).ThrowOnFailure();

    internal Packet(AVPacket* ptr) : base(ptr) { } // Allow owned packet from ptr when we clone

    #region Disposal
    ~Packet()
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
        fixed (AVPacket** _ptrPtr = &_ptr)
            av_packet_free(_ptrPtr);
    }
    #endregion
}
