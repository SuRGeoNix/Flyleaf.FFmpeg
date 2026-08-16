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
    public ReadOnlySpan<AVChannelLayout>    ChannelLayouts  => new(channelLayouts, channelLayoutsCount);
    readonly AVChannelLayout* channelLayouts;
    readonly int channelLayoutsCount;

    public ReadOnlySpan<AVSampleFormat>     SampleFormats   => new(sampleFormats, sampleFormatsCount);
    readonly AVSampleFormat* sampleFormats;
    readonly int sampleFormatsCount;

    public ReadOnlySpan<int>                SampleRates     => new(sampleRates, sampleRatesCount);
    readonly int* sampleRates;
    readonly int sampleRatesCount;

    internal AudioCodecSpec(AVCodec* codec) : base(codec)
    {
        fixed(AVChannelLayout** ptr1 = &channelLayouts)
            fixed(int* ptr2 = &channelLayoutsCount)
                _ = avcodec_get_supported_config(null, _ptr, AVCodecConfig.ChannelLayout, 0, (void**)ptr1, ptr2);

        fixed(AVSampleFormat** ptr1 = &sampleFormats)
            fixed(int* ptr2 = &sampleFormatsCount)
                _ = avcodec_get_supported_config(null, _ptr, AVCodecConfig.SampleFormat, 0, (void**)ptr1, ptr2);

        fixed(int** ptr1 = &sampleRates)
            fixed(int* ptr2 = &sampleRatesCount)
                _ = avcodec_get_supported_config(null, _ptr, AVCodecConfig.SampleRate, 0, (void**)ptr1, ptr2);
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
    public ReadOnlySpan<AVRational> FrameRates  => new(frameRates, frameRatesCount);
    readonly AVRational* frameRates;
    readonly int frameRatesCount;

    internal VideoCodecSpec(AVCodec* codec) : base(codec)
    {
        fixed(AVRational** ptr1 = &frameRates)
            fixed(int* ptr2 = &frameRatesCount)
                _ = avcodec_get_supported_config(null, _ptr, AVCodecConfig.FrameRate, 0, (void**)ptr1, ptr2);
    }
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
    public List<AVCodecHWConfig>        HWConfigs       { get; }
    public HWWrapper                    HWWrapper       { get; }
    
    public ReadOnlySpan<AVAlphaMode>    AlphaModes      => new(alphaModes, alphaModesCount);
    readonly AVAlphaMode* alphaModes;
    readonly int alphaModesCount;

    public ReadOnlySpan<AVPixelFormat>  PixelFormats    => new(pixelFormats, pixelFormatsCount);
    readonly AVPixelFormat* pixelFormats;
    readonly int pixelFormatsCount;

    internal VideoEncoderSpec(AVCodec* codec) : base(codec) 
    {
        fixed(AVAlphaMode** ptr1 = &alphaModes)
            fixed(int* ptr2 = &alphaModesCount)
                _ = avcodec_get_supported_config(null, _ptr, AVCodecConfig.AlphaMode, 0, (void**)ptr1, ptr2);

        fixed(AVPixelFormat** ptr1 = &pixelFormats)
            fixed(int* ptr2 = &pixelFormatsCount)
                _ = avcodec_get_supported_config(null, _ptr, AVCodecConfig.PixFormat, 0, (void**)ptr1, ptr2);

        HWConfigs   = GetHWConfigs(codec);

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