namespace Flyleaf.FFmpeg.Codec.Decode;

public unsafe abstract class Decoder : CodecContext
{
    // RW D VAS (TBR: maybe no S? fixed to 1/1000 ms)
    public AVRational           PacketTimebase      { get => _ptr->pkt_timebase;                set => _ptr->pkt_timebase = value; } // timebase for decoding is unused might use timebase prop name for this?

    #region SideData
    public AVPacketSideData*    PacketSideData          => _ptr->coded_side_data;
    public int                  PacketSideDataCount     => _ptr->nb_coded_side_data;

    public AVPacketSideData* SideDataGet(AVPacketSideDataType type)
        => av_packet_side_data_get(_ptr->coded_side_data, _ptr->nb_coded_side_data, type);

    public void SideDataCopyTo(AVPacketSideData** dstPtr, int* dstCount)
        => SideDataCopy(_ptr->coded_side_data,_ptr->nb_coded_side_data, dstPtr, dstCount);

    public AVPacketSideData* SideDataNew(AVPacketSideDataType type, nuint size)
        => av_packet_side_data_new(&_ptr->coded_side_data, &_ptr->nb_coded_side_data, type, size, 0);

    public AVPacketSideData* SideDataAdd(AVPacketSideDataType type, byte* data, nuint dataSize) // TBR: *byte / must be malloc*
        => av_packet_side_data_add(&_ptr->coded_side_data, &_ptr->nb_coded_side_data, type, data, dataSize, 0);

    public void SideDataRemove(AVPacketSideDataType type)
        => av_packet_side_data_remove(_ptr->coded_side_data, &_ptr->nb_coded_side_data, type);

    public void SideDataFree()
        => av_packet_side_data_free(&_ptr->coded_side_data, &_ptr->nb_coded_side_data);
    #endregion

    #region Frame Side Data
    public AVFrameSideData**    FrameSideData           => _ptr->decoded_side_data;
    public int                  FrameSideDataCount      => _ptr->nb_decoded_side_data;

    public AVFrameSideData* SideDataGet(AVFrameSideDataType type)
        => av_frame_side_data_get_c(_ptr->decoded_side_data, _ptr->nb_decoded_side_data, type);

    public FFmpegResult SideDataCopyTo(AVFrameSideData*** dstPtr, int* dstCount, FrameSideDataFlags flags = FrameSideDataFlags.None)
        => SideDataCopy(_ptr->decoded_side_data, _ptr->nb_decoded_side_data, dstPtr, dstCount, flags);
    #endregion

    protected Decoder(AVCodec* codec) : base(codec) { }
}
