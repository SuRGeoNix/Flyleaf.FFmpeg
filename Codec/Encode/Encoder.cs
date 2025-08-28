namespace Flyleaf.FFmpeg.Codec.Encode;

public unsafe abstract class Encoder : CodecContext
{
    #region SideData RO
    public AVPacketSideData*    PacketSideData          => _ptr->coded_side_data;
    public int                  PacketSideDataCount     => _ptr->nb_coded_side_data;

    public AVPacketSideData* SideDataGet(AVPacketSideDataType type)
        => av_packet_side_data_get(_ptr->coded_side_data, _ptr->nb_coded_side_data, type);

    public void SideDataCopyTo(AVPacketSideData** dstPtr, int* dstCount)
        => SideDataCopy(_ptr->coded_side_data,_ptr->nb_coded_side_data, dstPtr, dstCount);
    #endregion

    #region Frame Side Data
    public AVFrameSideData**    FrameSideData           => _ptr->decoded_side_data;
    public int                  FrameSideDataCount      => _ptr->nb_decoded_side_data;

    public AVFrameSideData* SideDataGet(AVFrameSideDataType type)
        => av_frame_side_data_get_c(_ptr->decoded_side_data, _ptr->nb_decoded_side_data, type);

    public FFmpegResult SideDataCopyTo(AVFrameSideData*** dstPtr, int* dstCount, FrameSideDataFlags flags = FrameSideDataFlags.None)
        => SideDataCopy(_ptr->decoded_side_data, _ptr->nb_decoded_side_data, dstPtr, dstCount, flags);

    public AVFrameSideData* SideDataNew(AVFrameSideDataType type, nuint size, FrameSideDataFlags flags = FrameSideDataFlags.None)
        => av_frame_side_data_new(&_ptr->decoded_side_data, &_ptr->nb_decoded_side_data, type, size, (uint)flags);

    public AVFrameSideData* SideDataAdd(AVFrameSideDataType type, AVBufferRef** buffer, FrameSideDataFlags flags = FrameSideDataFlags.None)
        => av_frame_side_data_add(&_ptr->decoded_side_data, &_ptr->nb_decoded_side_data, type, buffer, (uint)flags);

    public void SideDataRemove(AVFrameSideDataType type)
        => av_frame_side_data_remove(&_ptr->decoded_side_data, &_ptr->nb_decoded_side_data, type);

    public void SideDataRemoveByProps(AVSideDataProps props)
        => av_frame_side_data_remove_by_props(&_ptr->decoded_side_data, &_ptr->nb_decoded_side_data, props);

    public void SideDataFree()
        => av_frame_side_data_free(&_ptr->decoded_side_data, &_ptr->nb_decoded_side_data);
    #endregion

    protected Encoder(AVCodec* codec) : base(codec) { }
}
