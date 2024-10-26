namespace Flyleaf.FFmpeg.Spec;

public unsafe abstract partial class CodecSpec
{
    public FFmpegClassSpec?     AVClass         => FFmpegClassSpec.Get(_ptr->priv_class);

    public string               Name            { get; }
    public string?              LongName        { get; }
    public AVCodecID            CodecId         { get; }
    public CodecCapFlags        Capabilities    { get; }
    public List<CodecProfile>   Profiles        { get; }
    //public string?              WrapperName     { get; }
    //public AVMediaType          Type            { get; }
    
    public readonly AVCodec* _ptr;

    internal CodecSpec(AVCodec* codec)
    {
        _ptr            = codec;
        Name            = GetString(codec->name)!;
        LongName        = GetString(codec->long_name);
        //WrapperName     = GetString(codec->wrapper_name);
        CodecId         = codec->id;
        Capabilities    = codec->capabilities;
        Profiles        = GetProfiles(codec->profiles);

        CodecSpecByPtr.Add((nint)codec, this);
    }

    public static implicit operator AVCodec*(CodecSpec? spec)
        => spec != null ? spec._ptr : null;
}

public unsafe class AudioCodecSpec : CodecSpec
{
    public List<AVChannelLayout>    ChannelLayouts  { get; }
    public List<AVSampleFormat>     SampleFormats   { get; }
    public List<int>                SampleRates     { get; }

    internal AudioCodecSpec(AVCodec* codec) : base(codec)
    {
        // TODO: (AccessViolation) AVChannelLayout** test = null; int ret = 0; var res = avcodec_get_supported_config(null, _ptr, AVCodecConfig.ChannelLayout, 0, (void**)test, &ret);
        
        ChannelLayouts  = GetChannelLayouts(codec->ch_layouts);
        SampleFormats   = GetSampleFormats(codec->sample_fmts);
        SampleRates     = GetSampleRates(codec->supported_samplerates);
    }
}

public unsafe sealed class AudioDecoderSpec : AudioCodecSpec
{
    internal AudioDecoderSpec(AVCodec* codec) : base(codec)
    {
        AudioDecoderByName.Add(Name, this);
        AddToDicList(AudioDecodersById, codec->id, this);
    }
}

public unsafe sealed class AudioEncoderSpec : AudioCodecSpec
{
    internal AudioEncoderSpec(AVCodec* codec) : base(codec)
    {
        AudioEncoderByName.Add(Name, this);
        AddToDicList(AudioEncodersById, codec->id, this);
    }
}

public unsafe class VideoCodecSpec : CodecSpec
{
    public List<AVRational>         FrameRates      { get; }

    internal VideoCodecSpec(AVCodec* codec) : base(codec)
        => FrameRates = GetFrameRates(codec->supported_framerates);
}

public unsafe sealed class VideoDecoderSpec : VideoCodecSpec
{
    public List<AVCodecHWConfig>    HWConfigs       { get; }
    public HWWrapper                HWWrapper       { get; }
    public byte                     MaxLowres       { get; }
    
    internal VideoDecoderSpec(AVCodec* codec) : base(codec)
    {
        HWConfigs = GetHWDecoderConfigs(codec);
        MaxLowres = codec->max_lowres;
        VideoDecoderByName.Add(Name, this);
        AddToDicList(VideoDecodersById, codec->id, this);

        if (HWConfigs.Count > 0)
        {
            if (codec->wrapper_name != null)
            {
                var wrapperName = GetString(codec->wrapper_name);

                if (wrapperName == "qsv")
                    HWWrapper = HWWrapper.Intel;
                else if (wrapperName == "cuvid")
                    HWWrapper = HWWrapper.Nvidia;
                else if (wrapperName == "mediacodec")
                    HWWrapper = HWWrapper.MediaCodec;
                else if (wrapperName == "videotoolbox")
                    HWWrapper = HWWrapper.VideoToolbox;
                else
                    HWWrapper = HWWrapper.Other;
            }

            AddToDicList(HWVideoDecodersById, codec->id, this);
        }
        else if (codec->wrapper_name != null)
            HWWrapper = HWWrapper.Other;
    }
}

public unsafe sealed class VideoEncoderSpec : VideoCodecSpec
{
    public List<AVCodecHWConfig>    HWConfigs       { get; }
    public HWWrapper                HWWrapper       { get; }
    public List<AVPixelFormat>      PixelFormats    { get; }

    internal VideoEncoderSpec(AVCodec* codec) : base(codec) 
    {
        HWConfigs   = GetHWConfigs(codec);
        PixelFormats= GetPixelFormats(codec->pix_fmts); // Encoder only

        AddToDicList(VideoEncodersById, codec->id, this);
        VideoEncoderByName.Add(Name, this);

        if (HWConfigs.Count > 0)
        {
            if (codec->wrapper_name != null)
            {
                var wrapperName = GetString(codec->wrapper_name);

                if (wrapperName == "qsv")
                    HWWrapper = HWWrapper.Intel;
                else if (wrapperName == "nvenc")
                    HWWrapper = HWWrapper.Nvidia;
                else if (wrapperName == "amf")
                    HWWrapper = HWWrapper.Amd;
                else if (wrapperName == "vaapi")
                    HWWrapper = HWWrapper.VAAPI;
                else if (wrapperName == "d3d12va")
                    HWWrapper = HWWrapper.D3D12;
                else if (wrapperName == "videotoolbox")
                    HWWrapper = HWWrapper.VideoToolbox;
                else
                    HWWrapper = HWWrapper.Other;
            }

            AddToDicList(HWVideoEncodersById, codec->id, this);    
        }
        else if (codec->wrapper_name != null)
            HWWrapper = HWWrapper.Other;
    }
}

public unsafe sealed class SubtitleDecoderSpec : CodecSpec
{
    internal SubtitleDecoderSpec(AVCodec* codec) : base(codec)
    {
        SubtitleDecoderByName.Add(Name, this);
        AddToDicList(SubtitleDecodersById, codec->id, this);
    }
}

public unsafe sealed class SubtitleEncoderSpec : CodecSpec
{
    internal SubtitleEncoderSpec(AVCodec* codec) : base(codec)
    {
        SubtitleEncoderByName.Add(Name, this);
        AddToDicList(SubtitleEncodersById, codec->id, this);
    }
}

public unsafe sealed class DecoderSpec(AVCodec* codec) : CodecSpec(codec);
public unsafe sealed class EncoderSpec(AVCodec* codec) : CodecSpec(codec);

public enum HWWrapper
{
    None,
    Other,  // external software?*

    Amd,    // amf
    D3D12,  // d3d12va
    Intel,  // qsv
    MediaCodec, // android
    Nvidia, // cuvid / nvenc
    VAAPI,  // vaapi
    VideoToolbox, // apple
}