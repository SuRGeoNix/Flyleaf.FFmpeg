using Flyleaf.FFmpeg.Codec.Encode;

namespace Flyleaf.FFmpeg.Format.Mux;

public unsafe class VideoStreamMux : MediaStreamMux
{
    // TBR: Seems VA only
    public int                  Level               { get => _codecpar->level;                  set => _codecpar->level = value; }
    public CodecProfile         CodecProfile        { get => CodecDescriptor == null ? PROFILE_UNKNOWN : GetProfile(CodecDescriptor.Profiles, _codecpar->profile); set => _codecpar->profile = value.Profile; }

    public AVRational           AvgFrameRate        { get => _ptr->avg_frame_rate;              set => _ptr->avg_frame_rate = value; }
    
    public int                  VideoDelay          { get => _codecpar->video_delay;            set => _codecpar->video_delay = value; }

    public AVRational           SampleAspectRatio   { get => _codecpar->sample_aspect_ratio;    set => _codecpar->sample_aspect_ratio = value;}
    public AVRational           SampleAspectRatio2  { get => _ptr->sample_aspect_ratio;         set => _ptr->sample_aspect_ratio = value; }
    
    public AVChromaLocation     ChromaLocation      { get => _codecpar->chroma_location;        set => _codecpar->chroma_location = value; }
    public AVColorPrimaries     ColorPrimaries      { get => _codecpar->color_primaries;        set => _codecpar->color_primaries = value; }
    public AVColorRange         ColorRange          { get => _codecpar->color_range;            set => _codecpar->color_range = value; }
    public AVColorSpace         ColorSpace          { get => _codecpar->color_space;            set => _codecpar->color_space = value; }
    public AVColorTransferCharacteristic
                                ColorTransfer       { get => _codecpar->color_trc;              set => _codecpar->color_trc = value; }
    public AVFieldOrder         FieldOrder          { get => _codecpar->field_order;            set => _codecpar->field_order = value; }
    public AVPixelFormat        PixelFormat         { get => (AVPixelFormat)_codecpar->format;  set => _codecpar->format = (int)value; }
    public int                  Height              { get => _codecpar->height;                 set => _codecpar->height = value; }
    public int                  Width               { get => _codecpar->width;                  set => _codecpar->width = value; }

    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr, EV)!;
    public AVRational           FrameRate           => _codecpar->framerate; // maybe hide ro (any reason to show for mux?)
    public AVRational           RealFrameRate       => _ptr->r_frame_rate;

    // set rotation helper? etc...

    // Check required (eg. width/height/pixelformat etc...?)
    public VideoStreamMux(Muxer muxer, AVCodecID codecId) : base(muxer, AVMediaType.Video, codecId) { }
    public VideoStreamMux(Muxer muxer, VideoStream stream) : base(muxer, AVMediaType.Video, stream.CodecId)
        => PrepareFrom(stream);
    public VideoStreamMux(Muxer muxer, VideoEncoder encoder) : base(muxer, AVMediaType.Video, encoder.CodecSpec.CodecId)
        => PrepareFrom(encoder);

    /* TODO
     * 1. maybe choose whether to copy side data, metadata etc?
     * 2. in case of multiple calls make sure we free the prev data (or this should happen directly on properties*)
     * 3. possible store locally source stream/encoder
     * 4. Expose CopySideData (no pointers possible ref?)
     */
    public void PrepareFrom(VideoStream stream)
    {
        //avcodec_parameters_copy(AVStream->codecpar, stream.AVStream->codecpar);
        //AVStream->codecpar->codec_tag = 0;

        BitRate             = stream.BitRate;
        BitsPerCodedSample  = stream.BitsPerCodedSample;
        BitsPerRawSample    = stream.BitsPerRawSample;
        Disposition         = stream.Disposition;

        Timebase            = stream.Timebase;
        Level               = stream.Level;
        CodecProfile        = stream.CodecProfile;
        AvgFrameRate        = stream.AvgFrameRate;
        Metadata            = stream.Metadata;

        ChromaLocation      = stream.ChromaLocation;
        ColorPrimaries      = stream.ColorPrimaries;
        ColorRange          = stream.ColorRange;
        ColorSpace          = stream.ColorSpace;
        ColorTransfer       = stream.ColorTransfer;
        FieldOrder          = stream.FieldOrder;
        VideoDelay          = stream.VideoDelay;
        Width               = stream.Width;
        Height              = stream.Height;
        SampleAspectRatio   = stream.SampleAspectRatio;
        SampleAspectRatio2  = stream.SampleAspectRatio2;
        PixelFormat         = stream.PixelFormat;

        // TBR
        //codecpar->framerate = stream.FrameRate;

        stream.ExtraDataCopyTo(&_codecpar->extradata, &_codecpar->extradata_size);
        stream.SideDataCopyTo(&_ptr->codecpar->coded_side_data, &_ptr->codecpar->nb_coded_side_data);
    }

    public void PrepareFrom(VideoEncoder encoder)
    {
        // TODO: set encoder locally? framerate possiible*
        // we might get input also the encoder's input stream to get metadata/disposition etc.?

        BitRate             = encoder.BitRate;
        BitsPerCodedSample  = encoder.BitsPerCodedSample;
        BitsPerRawSample    = encoder.BitsPerRawSample;

        Timebase            = encoder.Timebase;
        Level               = encoder.Level;
        CodecProfile        = encoder.CodecProfile;

        AvgFrameRate        = encoder.FrameRate; //?
        ChromaLocation      = encoder.ChromaLocation;
        ColorPrimaries      = encoder.ColorPrimaries;
        ColorRange          = encoder.ColorRange;
        ColorSpace          = encoder.ColorSpace;
        ColorTransfer       = encoder.ColorTransfer;
        FieldOrder          = encoder.FieldOrder;
        VideoDelay          = encoder.VideoDelay;
        Width               = encoder.Width;
        Height              = encoder.Height;
        SampleAspectRatio   = encoder.SampleAspectRatio;
        
        // TBR: This might be required only for specific codecs/output formats
        var hwframes        = encoder.HWFramesContext;
        PixelFormat         = hwframes != null ? hwframes.SWPixelFormat : encoder.PixelFormat;

        encoder.ExtraDataCopyTo(&_codecpar->extradata, &_codecpar->extradata_size);
        encoder.SideDataCopyTo(&_ptr->codecpar->coded_side_data, &_ptr->codecpar->nb_coded_side_data);
    }
}
