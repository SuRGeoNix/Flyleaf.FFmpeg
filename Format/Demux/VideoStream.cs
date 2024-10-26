namespace Flyleaf.FFmpeg.Format.Demux;

public unsafe class VideoStream : MediaStream
{
    public int                  Level               => _codecpar->level;
    public CodecProfile         CodecProfile        => CodecDescriptor == null ? PROFILE_UNKNOWN : GetProfile(CodecDescriptor.Profiles, _codecpar->profile);

    public AVRational           AvgFrameRate        => _ptr->avg_frame_rate;
    public AVRational           FrameRate           => _codecpar->framerate;
    public AVRational           RealFrameRate       => _ptr->r_frame_rate;
    public AVRational           GuessedFrameRate    => av_guess_frame_rate(null, _ptr, null); // both format and frame are unused!

    public int                  VideoDelay          => _codecpar->video_delay;

    // TBR: Common properties VFrame + ED V (Codec)
    public AVRational           SampleAspectRatio   => _codecpar->sample_aspect_ratio;
    public AVRational           SampleAspectRatio2  => _ptr->sample_aspect_ratio;
    public AVRational           GuessedSampleAspectRatio
                                                    => av_guess_sample_aspect_ratio(null, _ptr, null);

    public AVChromaLocation     ChromaLocation      => _codecpar->chroma_location;
    public AVColorPrimaries     ColorPrimaries      => _codecpar->color_primaries;
    public AVColorRange         ColorRange          => _codecpar->color_range;
    public AVColorSpace         ColorSpace          => _codecpar->color_space;
    public AVColorTransferCharacteristic
                                ColorTransfer       => _codecpar->color_trc;
    public AVFieldOrder         FieldOrder          => _codecpar->field_order;
    public AVPixelFormat        PixelFormat         => (AVPixelFormat)_codecpar->format;
    public int                  Height              => _codecpar->height;
    public int                  Width               => _codecpar->width;

    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr, DV)!;

    public double GetRotation()
    {
        if ( _ptr->codecpar->nb_coded_side_data == 0)
            return 0;

        AVPacketSideData* displayMatrixPtr = av_packet_side_data_get(_ptr->codecpar->coded_side_data, _ptr->codecpar->nb_coded_side_data, AVPacketSideDataType.Displaymatrix);
        if (displayMatrixPtr == null || displayMatrixPtr->data == null)
            return 0;
        
        int_array9 displayMatrix = PtrToStructure<int_array9>((nint)displayMatrixPtr->data);
        return av_display_rotation_get(displayMatrix);
    }

    public AVRational GuessSampleAspectRatio()
        => GuessSampleAspectRatio((AVFrame*)null);

    public AVRational GuessSampleAspectRatio(VideoFrame frame)
        => GuessSampleAspectRatio(frame._ptr);

    public AVRational GuessSampleAspectRatio(AVFrame* frame)
        => av_guess_sample_aspect_ratio(null, _ptr, frame); // format unused

    public AVRational GetDisplayAspectRatio(AVRational? sampleAspectRatio = null)
    {
        int x, y;
        AVRational sar = sampleAspectRatio ?? GuessSampleAspectRatio();
        
        if (sar.Num < 1 || sar.Den < 1)
            sar = new(1, 1);

        _ = av_reduce(&x, &y, Width * sar.Num, Height * sar.Den, 1024 * 1024);
   
        return new(x, y);
    }

    internal VideoStream(FormatContext fmt, AVStream* stream) : base(fmt, stream) { }
}
