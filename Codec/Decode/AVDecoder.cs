namespace Flyleaf.FFmpeg.Codec.Decode;

public unsafe abstract class AVDecoder : Decoder
{
    public int GetBufferDefault(AVFrame* frame, int flags) => avcodec_default_get_buffer2(_ptr, frame, flags);
    AVCodecContext_get_buffer2? GetBuffer2Dlgt;

    protected AVDecoder(AVCodec* codec, AVCodecContext_get_buffer2? getBufferClbk = null) : base(codec)
    {
        if (getBufferClbk != null)
        {
            GetBuffer2Dlgt  = getBufferClbk;
            _ptr->get_buffer2= GetBuffer2Dlgt;
        }
    }

    public FFmpegResult SendPacket(AVPacket* pkt)
        => new(avcodec_send_packet(_ptr, pkt));

    public FFmpegResult SendPacket(PacketBase pkt)
        => new(avcodec_send_packet(_ptr, pkt));

    public FFmpegResult Drain()
        => new(avcodec_send_packet(_ptr, null));

    public FFmpegResult RecvFrame(AVFrame* frame)
        => new(avcodec_receive_frame(_ptr, frame));
}
