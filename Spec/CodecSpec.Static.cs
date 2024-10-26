namespace Flyleaf.FFmpeg.Spec;

public unsafe abstract partial class CodecSpec
{
    #region CodecSpec Helpers
    public static AudioDecoderSpec?         FindAudioDecoder        (string name)   => AudioDecoderByName.TryGetValue(name, out var codec) ? codec : null;
    public static AudioEncoderSpec?         FindAudioEncoder        (string name)   => AudioEncoderByName.TryGetValue(name, out var codec) ? codec : null;
    public static VideoDecoderSpec?         FindVideoDecoder        (string name)   => VideoDecoderByName.TryGetValue(name, out var codec) ? codec : null;
    public static VideoEncoderSpec?         FindVideoEncoder        (string name)   => VideoEncoderByName.TryGetValue(name, out var codec) ? codec : null;
    public static SubtitleDecoderSpec?      FindSubtitleDecoder     (string name)   => SubtitleDecoderByName.TryGetValue(name, out var codec) ? codec : null;
    public static SubtitleEncoderSpec?      FindSubtitleEncoder     (string name)   => SubtitleEncoderByName.TryGetValue(name, out var codec) ? codec : null;
    public static AudioDecoderSpec?         FindAudioDecoder        (AVCodecID id)  => AudioDecodersById.TryGetValue(id, out var codec) ? codec[0] : null;
    public static List<AudioDecoderSpec>?   FindAudioDecoders       (AVCodecID id)  => AudioDecodersById.TryGetValue(id, out var codec) ? codec : null;
    public static AudioEncoderSpec?         FindAudioEncoder        (AVCodecID id)  => AudioEncodersById.TryGetValue(id, out var codec) ? codec[0] : null;
    public static List<AudioEncoderSpec>?   FindAudioEncoders       (AVCodecID id)  => AudioEncodersById.TryGetValue(id, out var codec) ? codec : null;
    public static VideoDecoderSpec?         FindVideoDecoder        (AVCodecID id)  => VideoDecodersById.TryGetValue(id, out var codec) ? codec[0] : null;
    public static List<VideoDecoderSpec>?   FindVideoDecoders       (AVCodecID id)  => VideoDecodersById.TryGetValue(id, out var codec) ? codec : null;
    public static VideoDecoderSpec?         FindHWVideoDecoder      (AVCodecID id)  => HWVideoDecodersById.TryGetValue(id, out var codec) ? codec[0] : null;
    public static List<VideoDecoderSpec>?   FindHWVideoDecoders     (AVCodecID id)  => HWVideoDecodersById.TryGetValue(id, out var codec) ? codec : null;
    public static VideoDecoderSpec?         FindHWVideoDecoder      (AVCodecID id, AVPixelFormat pixelFormat) => FindHWVideoDecoderHelper(id, pixelFormat);
    public static List<VideoDecoderSpec>?   FindHWVideoDecoders     (AVCodecID id, AVPixelFormat pixelFormat) => FindHWVideoDecodersHelper(id, pixelFormat);
    public static VideoEncoderSpec?         FindVideoEncoder        (AVCodecID id)  => VideoEncodersById.TryGetValue(id, out var codec) ? codec[0] : null;
    public static List<VideoEncoderSpec>?   FindVideoEncoders       (AVCodecID id)  => VideoEncodersById.TryGetValue(id, out var codec) ? codec : null;
    public static SubtitleDecoderSpec?      FindSubtitleDecoder     (AVCodecID id)  => SubtitleDecodersById.TryGetValue(id, out var codec) ? codec[0] : null;
    public static List<SubtitleDecoderSpec>?FindSubtitleDecoders    (AVCodecID id)  => SubtitleDecodersById.TryGetValue(id, out var codec) ? codec : null;
    public static SubtitleEncoderSpec?      FindSubtitleEncoder     (AVCodecID id)  => SubtitleEncodersById.TryGetValue(id, out var codec) ? codec[0] : null;
    public static List<SubtitleEncoderSpec>?FindSubtitleEncoders    (AVCodecID id)  => SubtitleEncodersById.TryGetValue(id, out var codec) ? codec : null;
    public static CodecSpec?                FindCodec(AVCodec* codec) => CodecSpecByPtr.TryGetValue((nint)codec, out var spec) ? spec : null;

    static VideoDecoderSpec? FindHWVideoDecoderHelper(AVCodecID id, AVPixelFormat pixelFormat)
    {
        if (HWVideoDecodersById.TryGetValue(id, out var codecs))
            foreach(var codec in codecs)
                foreach(var hwconfig in codec.HWConfigs)
                    if (hwconfig.pix_fmt == pixelFormat)
                        return codec;

        return null;
    }

    static List<VideoDecoderSpec>? FindHWVideoDecodersHelper(AVCodecID id, AVPixelFormat pixelFormat)
    {
        List<VideoDecoderSpec> decoders = [];
        if (HWVideoDecodersById.TryGetValue(id, out var codecs))
            foreach(var codec in codecs)
                foreach(var hwconfig in codec.HWConfigs)
                    if (hwconfig.pix_fmt == pixelFormat)
                        { decoders.Add(codec); break; }

        return decoders.Count > 0 ? decoders : null;
    }
    #endregion

    #region Maps Initializer
    internal static void FillCodecSpecs()
    {
        AVCodec* avcodec;
        void* opaque = null;
        while ((avcodec = av_codec_iterate(ref opaque)) != null)
            _ = Get(avcodec);
    }

    static CodecSpec Get(AVCodec* codec) => codec->type switch
    {
        AVMediaType.Audio   => av_codec_is_decoder(codec) != 0 ? new AudioDecoderSpec(codec)    : new AudioEncoderSpec(codec),
        AVMediaType.Video   => av_codec_is_decoder(codec) != 0 ? new VideoDecoderSpec(codec)    : new VideoEncoderSpec(codec),
        AVMediaType.Subtitle=> av_codec_is_decoder(codec) != 0 ? new SubtitleDecoderSpec(codec) : new SubtitleEncoderSpec(codec),
        _                   => av_codec_is_decoder(codec) != 0 ? new DecoderSpec(codec)         : new EncoderSpec(codec)
    };

    // TBR: Consider Frozen (much slower startup, a bit faster reads)
    // Expose lists for fast iterate and search (eg. for hwconfigs)
    internal static Dictionary<nint, CodecSpec>                         CodecSpecByPtr = [];

    internal static Dictionary<AVCodecID, List<AudioDecoderSpec>>       AudioDecodersById = [];
    internal static Dictionary<AVCodecID, List<AudioEncoderSpec>>       AudioEncodersById = [];
    internal static Dictionary<AVCodecID, List<VideoDecoderSpec>>       VideoDecodersById = [];
    internal static Dictionary<AVCodecID, List<VideoEncoderSpec>>       VideoEncodersById = [];
    internal static Dictionary<AVCodecID, List<SubtitleDecoderSpec>>    SubtitleDecodersById = [];
    internal static Dictionary<AVCodecID, List<SubtitleEncoderSpec>>    SubtitleEncodersById = [];

    internal static Dictionary<AVCodecID, List<VideoDecoderSpec>>       HWVideoDecodersById = [];
    internal static Dictionary<AVCodecID, List<VideoEncoderSpec>>       HWVideoEncodersById = [];

    internal static Dictionary<string, AudioDecoderSpec>                AudioDecoderByName = [];
    internal static Dictionary<string, AudioEncoderSpec>                AudioEncoderByName = [];
    internal static Dictionary<string, VideoDecoderSpec>                VideoDecoderByName = [];
    internal static Dictionary<string, VideoEncoderSpec>                VideoEncoderByName = [];
    internal static Dictionary<string, SubtitleDecoderSpec>             SubtitleDecoderByName = [];
    internal static Dictionary<string, SubtitleEncoderSpec>             SubtitleEncoderByName = [];
    #endregion
}
