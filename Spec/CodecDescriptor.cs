namespace Flyleaf.FFmpeg.Spec;

public unsafe abstract partial class CodecDescriptor
{
    public string               Name        { get; }
    public string?              LongName    { get; }
    public List<string>         MimeTypes   { get; }
    public List<CodecProfile>   Profiles    { get; }
    public CodecPropFlags       Properties  { get; }
    public AVCodecID            CodecId     { get; }
    public AVMediaType          CodecType   { get; } // for easier access than is?

    internal CodecDescriptor(AVCodecDescriptor* codec)
    {
        Name        = GetString(codec->name)!;
        LongName    = GetString(codec->long_name);
        MimeTypes   = GetMimeTypes(codec->mime_types);
        Properties  = codec->props;
        Profiles    = GetProfiles(codec->profiles);
        CodecId     = codec->id;
        CodecType   = codec->type;

        CodecDescriptorByCodecId.Add(CodecId, this);
    }
}

public unsafe sealed class AudioCodecDescriptor : CodecDescriptor
{
    internal AudioCodecDescriptor(AVCodecDescriptor* codec) : base(codec) { }
}

public unsafe sealed class VideoCodecDescriptor : CodecDescriptor
{
    internal VideoCodecDescriptor(AVCodecDescriptor* codec) : base(codec) { }
}

public unsafe sealed class SubtitleCodecDescriptor : CodecDescriptor
{
    internal SubtitleCodecDescriptor(AVCodecDescriptor* codec) : base(codec) { }
}

public unsafe sealed class DataCodecDescriptor : CodecDescriptor
{
    internal DataCodecDescriptor(AVCodecDescriptor* codec) : base(codec) { }
}