namespace Flyleaf.FFmpeg.Codec.Encode;

public unsafe abstract class AVEncoder : Encoder
{
    // RW E AV
    public AVRational           Timebase                { get => _ptr->time_base;                   set => _ptr->time_base = value; } // (required)

    protected AVEncoder(AVCodec* codec) : base(codec) { }

    public FFmpegResult SendFrame(AVFrame* frame)
        => new(avcodec_send_frame(_ptr, frame));

    public FFmpegResult Drain()
        => new(avcodec_send_frame(_ptr, null));

    public int RecvPacket(AVPacket* pkt)
        => avcodec_receive_packet(_ptr, pkt);

    public FFmpegResult RecvPacket(PacketBase pkt)
        => new(avcodec_receive_packet(_ptr, pkt));

    // TBR: generally provide also option to local allocate them?
    //public int GetPacket(out AVPacket* pkt)
    //{
    //    pkt = av_packet_alloc();
    //    return avcodec_receive_packet(ctx, pkt);
    //}

    // TBR: AV_CODEC_FLAG_RECON_FRAME (reconstructed frame - preview of the frame that will be after decoding the encoded pkt - last success GetPacket*)
    public FFmpegResult RecvFrame(AVFrame* frame)
        => new(avcodec_receive_frame(_ptr, frame));

}
