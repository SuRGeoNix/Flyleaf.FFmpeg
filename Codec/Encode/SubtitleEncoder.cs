namespace Flyleaf.FFmpeg.Codec.Encode;

public unsafe class SubtitleEncoder : Encoder
{
    #region Configuration Properties (RW)
    public SubtitleEncoderFlags         Flags                   { get => (SubtitleEncoderFlags)_ptr->flags;                 set => _ptr->flags = (CodecFlags)value; }
    public VASEncoderExportDataFlags    ExportSideDataFlags     { get => (VASEncoderExportDataFlags)_ptr->export_side_data; set => _ptr->export_side_data = (CodecExportDataFlags)value; }

    // ASS specific*
    public byte*                        Header                  { get => _ptr->subtitle_header;                             set => _ptr->subtitle_header = value; }
    public int                          HeaderSize              { get => _ptr->subtitle_header_size;                        set => _ptr->subtitle_header_size = value; }
    

    public AVRational                   Timebase                { get => _ptr->time_base;                                   set => _ptr->time_base = value; } // (required) that is default to 1/1000? ms
    public long                         BitRate                 { get => _ptr->bit_rate;                                    set => _ptr->bit_rate = value; } //(might overwritten) seems possible to set also for the decoder?

    public int                          Width                   { get => _ptr->width;                                       set => _ptr->width = value; }
    public int                          Height                  { get => _ptr->height;                                      set => _ptr->height = value; }
    #endregion

    public FFmpegClass                  AVClass                 => FFmpegClass.Get(_ptr, ES)!;
    public SubtitleEncoderSpec          CodecSpec               { get; }
    public long                         FrameNumber             => _ptr->frame_num;

    public SubtitleEncoder(SubtitleEncoderSpec codec) : base(codec) { CodecSpec = codec; }
    
    public FFmpegResult SendRecvPacket(SubtitleFrame frame, out Packet pkt)
    {
        FFmpegResult ret = SendRecvPacket(frame._ptr, out AVPacket* avpkt);
        pkt = new(avpkt);
        return ret;
    }

    static int SubtitleMaxPacketSize = 1024 * 1024;
    public FFmpegResult SendRecvPacket(AVSubtitle* frame, out AVPacket* pkt)
    {
        // TODO: multiple packets for AV_CODEC_ID_DVB_SUBTITLE (2 one to clear / AV_CODEC_ID_ASS (at least 1 and equal to rects)
        pkt = av_packet_alloc();

        FFmpegResult pktSize;
        pktSize = new(av_new_packet(pkt, SubtitleMaxPacketSize));
        if (pktSize.Failed)
            return pktSize;

        frame->pts              += frame->start_display_time * 1000L;
        frame->end_display_time -= frame->start_display_time;
        frame->start_display_time= 0;
        
        pktSize = new(avcodec_encode_subtitle(_ptr, pkt->data, pkt->size, frame));
        if (pktSize.Failed)
            return pktSize;

        av_shrink_packet(pkt, pktSize.Result);
        pkt->dts = pkt->pts = frame->pts;

        if (frame->end_display_time != uint.MaxValue)
            pkt->duration = frame->end_display_time * 1000L;

        return pktSize;
    }
}
