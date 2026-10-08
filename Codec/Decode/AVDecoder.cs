namespace Flyleaf.FFmpeg.Codec.Decode;

public unsafe abstract class AVDecoder : Decoder
{
    public int GetBufferDefault(AVFrame* frame, int flags) => avcodec_default_get_buffer2(_ptr, frame, flags);
    AVCodecContext.GetBuffer2? GetBuffer2Dlgt;

    protected AVDecoder(AVCodec* codec, AVCodecContext.GetBuffer2? getBufferClbk = null) : base(codec)
    {
        if (getBufferClbk != null)
        {
            GetBuffer2Dlgt  = getBufferClbk;
            _ptr->get_buffer2= GetBuffer2Dlgt;
        }
    }

    public FFmpegResult SendPacket(AVPacket* pkt)
        => new(avcodec_send_packet(_ptr, pkt));

    public FFmpegResult SendPacket(Packet pkt)
        => new(avcodec_send_packet(_ptr, pkt));

    public FFmpegResult Drain()
        => new(avcodec_send_packet(_ptr, null));

    public FFmpegResult RecvFrame(AVFrame* frame)
        => new(avcodec_receive_frame_flags(_ptr, frame, 0));

    public FFmpegResult RecvFrameSync(AVFrame* frame) // TBR: Currently only Sync flag for decoding | Fix flags in Bindings
        => new(avcodec_receive_frame_flags(_ptr, frame, AV_CODEC_RECEIVE_FRAME_FLAG_SYNCHRONOUS));
}
