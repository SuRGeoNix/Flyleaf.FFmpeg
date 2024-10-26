using Flyleaf.FFmpeg.Codec.Encode;

namespace Flyleaf.FFmpeg.Format.Mux;

public unsafe class SubtitleStreamMux : MediaStreamMux
{
    public CodecProfile         CodecProfile        { get => CodecDescriptor == null ? PROFILE_UNKNOWN : GetProfile(CodecDescriptor.Profiles, _codecpar->profile); set => _codecpar->profile = value.Profile; }
    public int                  Height              { get => _codecpar->height;  set => _codecpar->height = value; }
    public int                  Width               { get => _codecpar->width;   set => _codecpar->width = value; }

    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr, ES)!;

    public SubtitleStreamMux(Muxer muxer, AVCodecID codecId) : base(muxer, AVMediaType.Subtitle, codecId) { }
    public SubtitleStreamMux(Muxer muxer, SubtitleStream stream, AVCodecID forceCodecId = AVCodecID.None) : base(muxer, AVMediaType.Subtitle, forceCodecId != AVCodecID.None ? forceCodecId : stream.CodecId) // TBR
        => PrepareFrom(stream);
    public SubtitleStreamMux(Muxer muxer, SubtitleEncoder encoder) : base(muxer, AVMediaType.Subtitle, encoder.CodecSpec.CodecId)
        => PrepareFrom(encoder);

    private void PrepareFrom(SubtitleStream stream)
    {
        BitRate             = stream.BitRate;
        BitsPerCodedSample  = stream.BitsPerCodedSample;
        BitsPerRawSample    = stream.BitsPerRawSample;
        Disposition         = stream.Disposition;

        Timebase            = stream.Timebase;
        Metadata            = stream.Metadata;

        Width               = stream.Width;
        Height              = stream.Height;

        stream.ExtraDataCopyTo(&_codecpar->extradata, &_codecpar->extradata_size);
        stream.SideDataCopyTo(&_ptr->codecpar->coded_side_data, &_ptr->codecpar->nb_coded_side_data);
    }

    private void PrepareFrom(SubtitleEncoder encoder)
    {
        BitRate             = encoder.BitRate;
        Timebase            = encoder.Timebase;

        Width               = encoder.Width;
        Height              = encoder.Height;
        
        encoder.ExtraDataCopyTo(&_codecpar->extradata, &_codecpar->extradata_size);
        encoder.SideDataCopyTo(&_ptr->codecpar->coded_side_data, &_ptr->codecpar->nb_coded_side_data);
    }
}
