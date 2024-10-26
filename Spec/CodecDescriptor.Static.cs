namespace Flyleaf.FFmpeg.Spec;

public unsafe partial class CodecDescriptor
{
    public static CodecDescriptor? FindCodecDescriptor(AVCodecID codecId)
        => CodecDescriptorByCodecId.TryGetValue(codecId, out var codec) ? codec : null;
    
    //public static List<CodecDescriptor> CodecDescriptors = [];
    public static Dictionary<AVCodecID, CodecDescriptor> CodecDescriptorByCodecId = [];

    internal static void FillCodecDescriptors()
    {
        AVCodecDescriptor* cur = null;
        while ((cur = avcodec_descriptor_next(cur)) != null)
            _ = Get(cur);
        //{
        //    var codec = Get(cur);
        //    CodecDescriptorByCodecId.Add(cur->id, codec);
        //    //CodecDescriptors.Add(codec);
        //}
    }
    static CodecDescriptor Get(AVCodecDescriptor* codec) => codec->type switch
    {
        AVMediaType.Audio   => new AudioCodecDescriptor(codec),
        AVMediaType.Video   => new VideoCodecDescriptor(codec),
        AVMediaType.Subtitle=> new SubtitleCodecDescriptor(codec),
        AVMediaType.Data    => new DataCodecDescriptor(codec),
        _                   => throw new NotSupportedException()
    };
}
