namespace Flyleaf.FFmpeg.Codec.Decode;

public unsafe class SubtitleDecoder : Decoder
{
    #region Configuration Properties (RW)
    public SubtitleDecoderFlags         Flags                   { get => (SubtitleDecoderFlags)_ptr->flags;                 set => _ptr->flags = (CodecFlags)value; }
    public SubtitleDecoderFlags2        Flags2                  { get => (SubtitleDecoderFlags2)_ptr->flags2;               set => _ptr->flags = (CodecFlags)value; }
    public VASEncoderExportDataFlags    ExportSideDataFlags     { get => (VASEncoderExportDataFlags)_ptr->export_side_data; set => _ptr->export_side_data = (CodecExportDataFlags)value; }
    public string?                      SubCharenc              { get => GetString(_ptr->sub_charenc);                      set => AVStrDupReplace(value, &_ptr->sub_charenc); }
    public SubCharencModeFlags          SubCharencModeFlags     { get => _ptr->sub_charenc_mode;                            set => _ptr->sub_charenc_mode = value; }

    // TBR if required
    public CodecProfile                 CodecProfile            { get => GetProfile(CodecSpec.Profiles, _ptr->profile);     set => _ptr->profile = value.Profile; } // for arib only?
    public int                          Width                   { get => _ptr->width;                                       set => _ptr->width = value; } // (might overwritten)
    public int                          Height                  { get => _ptr->height;                                      set => _ptr->height = value; } // (might overwritten)
    #endregion

    public SubtitleDecoderSpec          CodecSpec               { get; }
    public long                         FrameNumber             => _ptr->frame_num;

    // ASS specific (seems same with Extradata)*
    public byte*                        Header                  => _ptr->subtitle_header;
    public int                          HeaderSize              => _ptr->subtitle_header_size;

    public void HeaderDataCopyTo(byte** dstPtr, int* dstSize)
        => HeaderDataCopy(_ptr->subtitle_header, _ptr->subtitle_header_size, dstPtr, dstSize);

    public FFmpegClass                  AVClass                 => FFmpegClass.Get(_ptr, DS)!;

    public SubtitleDecoder(SubtitleDecoderSpec codec, SubtitleStream? stream = null) : base(codec)
    {
        CodecSpec = codec;

        if (stream != null)
            PrepareFrom(stream);
    }
    
    public (FFmpegResult, bool) SendRecvFrame(Packet pkt, SubtitleFrame frame)
        => SendRecvFrame(pkt._ptr, frame._ptr);

    public (FFmpegResult, bool) SendRecvFrame(AVPacket* pkt, AVSubtitle* frame)
    {
        int gotFrame;
        FFmpegResult ret = new(avcodec_decode_subtitle2(_ptr, frame, &gotFrame, pkt)); // NOTE: it will memset 0 sub + always sets frame->pts to TIME_BASE_Q
        
        return (ret, gotFrame != 0);
    }

    public (FFmpegResult, bool) Drain(AVSubtitle* frame)
        => SendRecvFrame(null, frame);

    public void PrepareFrom(SubtitleStream stream)
    {
        PacketTimebase  = stream.Timebase;
        CodecTag        = stream.CodecTag; // Note: this is not necessary related to codecId / codec
        Width           = stream.Width;
        Height          = stream.Height;

        stream.ExtraDataCopyTo(&_ptr->extradata, &_ptr->extradata_size);
        stream.SideDataCopyTo(&_ptr->coded_side_data, &_ptr->nb_coded_side_data);
    }
}
