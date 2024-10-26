namespace Flyleaf.FFmpeg.Spec;

// NOTE: AV_CODEC_ID_FIRST_.... are dummy

public unsafe abstract class FormatSpec
{
    public static DemuxerSpec?  FindDemuxerByName(string name)  => DemuxerByName.TryGetValue(name, out var fmt) ? fmt : null;
    public static MuxerSpec?    FindMuxerByname(string name)    => MuxerByName.TryGetValue(name, out var fmt) ? fmt : null;
    public static DemuxerSpec   FindDemuxer(AVInputFormat* fmt) => (DemuxerSpec)FormatSpecByPtr[(nint)fmt];
    public static MuxerSpec     FindMuxer(AVOutputFormat* fmt)  => (MuxerSpec)FormatSpecByPtr[(nint)fmt];

    public static List<uint>?   FindTags(AVCodecID codecId)     => TagsById.TryGetValue(codecId, out var tag) ? tag : null;
    public static uint?         FindTag(AVCodecID codecId)      => TagsById.TryGetValue(codecId, out var tag) ? tag[0] : null;
    
    // TBR: Readonly expose
    public static List<DemuxerSpec>                 DemuxerSpecs = [];
    public static List<MuxerSpec>                   MuxerSpecs = [];

    public static Dictionary<nint, FormatSpec>      FormatSpecByPtr = [];
    public static Dictionary<string, DemuxerSpec>   DemuxerByName = [];
    public static Dictionary<string, MuxerSpec>     MuxerByName = [];

    public static Dictionary<uint, List<FormatSpec>>FormatSpecsByTag = []; // TBR
    public static Dictionary<AVCodecID, List<uint>> TagsById = [];

    internal static void FillFormatSpecs()
    {
        void* opaque = null;
        AVInputFormat* ifmt;

        while((ifmt = av_demuxer_iterate(ref opaque)) != null)
        {
            var spec = new DemuxerSpec(ifmt);
            FormatSpecByPtr.Add((nint)ifmt, spec);
            DemuxerSpecs.Add(spec);
            FillTags(spec, spec.CodecTags);
        }

        AVOutputFormat* ofmt;
        opaque = null;
        while((ofmt = av_muxer_iterate(ref opaque)) != null)
        {
            var spec = new MuxerSpec(ofmt);
            FormatSpecByPtr.Add((nint)ofmt, spec);
            MuxerSpecs.Add(spec);
            FillTags(spec, spec.CodecTags);
        }
    }

    static void FillTags(FormatSpec fmtSpec, List<AVCodecTag> tags)
    {
        foreach(var tag in tags)
        {
            AddToDicList(TagsById, tag.id, tag.tag);
            AddToDicList(FormatSpecsByTag, tag.tag, fmtSpec);
        }
    }
}

public unsafe sealed class DemuxerSpec : FormatSpec
{
    public FFmpegClassSpec? AVClass             => FFmpegClassSpec.Get(_ptr->priv_class);
    public string           Name                { get; }
    public string?          LongName            { get; }
    public string[]?        Extenstions         { get; }
    public string[]?        MimeType            { get; }
    public DemuxerSpecFlags Flags               { get; }
    public List<AVCodecTag> CodecTags           { get; } // + AVSType, FourCC

    public readonly AVInputFormat* _ptr;

    public DemuxerSpec(AVInputFormat* fmt)
    {
        _ptr   = fmt;
        Name            = GetString(fmt->name)!;
        LongName        = GetString(fmt->long_name);
        Extenstions     = GetStringsFromComma(fmt->extensions);
        MimeType        = GetStringsFromComma(fmt->mime_type);
        Flags           = (DemuxerSpecFlags)fmt->flags;
        CodecTags       = GetTags(fmt->codec_tag);

        DemuxerByName.Add(Name, this);
    }

    public static implicit operator AVInputFormat*(DemuxerSpec spec) => spec._ptr;

    public static Tuple<DemuxerSpec?, int> FindDemuxer(IOContext ctx, string? url = null, uint offset = 0, uint maxProbeSize = 0)
    {
        AVInputFormat* fmt;
        int probeScore = av_probe_input_buffer2(ctx, &fmt, url, ctx, offset, maxProbeSize); // logctx this? (ctx->priv/opaque for urlcontext or parent such as avformatcontext?)

        if (fmt == null)
            return new(null, 0);

        return new(fmt == null ? null : (DemuxerSpec)FormatSpecByPtr[(nint)fmt], probeScore);
    }

    public static Tuple<DemuxerSpec?, int> FindDemuxer(string? filename = null, string? mimeType = null) // TODO: buf data?
    {
        AVProbeData pd = new()
        {
            filename    = av_strdup(filename),
            mime_type   = av_strdup(mimeType)
        };

        int score;
        AVInputFormat* fmt = av_probe_input_format3(&pd, 1, &score);

        // ffmpeg will not free them?
        av_free(pd.filename);
        av_free(pd.mime_type);

        return new(fmt == null ? null : (DemuxerSpec)FormatSpecByPtr[(nint)fmt], score);
    }
}

public unsafe sealed class MuxerSpec : FormatSpec
{
    public FFmpegClassSpec? AVClass             => FFmpegClassSpec.Get(_ptr->priv_class);
    public string           Name                { get; }
    public string?          LongName            { get; }
    public string[]?        Extenstions         { get; }
    public string?          MimeType            { get; }
    public MuxerSpecFlags   Flags               { get; }
    public List<AVCodecTag> CodecTags           { get; } // + AVSType, FourCC
    public AVCodecID        VideoEncoder        { get; }
    public AVCodecID        SubtitleEncoder     { get; }
    public AVCodecID        AudioEncoder        { get; }
    public AVCodecID        DataEncoder         { get; }

    public readonly AVOutputFormat* _ptr;

    public MuxerSpec(AVOutputFormat* fmt)
    {
        _ptr  = fmt;
        Name            = GetString(fmt->name)!;
        LongName        = GetString(fmt->long_name);
        Extenstions     = GetStringsFromComma(fmt->extensions);
        MimeType        = GetString(fmt->mime_type);
        Flags           = (MuxerSpecFlags)fmt->flags;
        CodecTags       = GetTags(fmt->codec_tag);
        VideoEncoder    = fmt->video_codec;
        SubtitleEncoder = fmt->subtitle_codec;
        AudioEncoder    = fmt->audio_codec;

        if (MuxerByName.ContainsKey(Name))
            MuxerByName.Add(Name +"_audio", this); // TBR: matroska (video/audio) same name really?*
        else
            MuxerByName.Add(Name, this);
    }

    public bool Supports(List<AVCodecID> codecIds)
    {
        int codecsCount = codecIds.Count;

        for (int i = 0; i < codecsCount; i++)
            if (!Supports(codecIds[i]))
                return false;

        return true;
    }

    public bool Supports(AVCodecID codecId)
    {
        if (VideoEncoder == codecId || AudioEncoder == codecId || SubtitleEncoder == codecId || DataEncoder == codecId)
            return true;
        else
        {
            for (int l = 0; l < CodecTags.Count; l++)
                if (CodecTags[l].id == codecId)
                    return true;
        }

        return false;
    }

    public int Supports2(AVCodecID codecId) // TBR
        => avformat_query_codec(_ptr, codecId, 0);

    public AVCodecID BestTextSubtitleEncoder()
    {
        if (SubtitleEncoder != AVCodecID.None && CodecDescriptorByCodecId.TryGetValue(SubtitleEncoder, out var encoder) && encoder.Properties.HasFlag(CodecPropFlags.TextSub))
            return SubtitleEncoder;

        for (int i = 0; i < CodecTags.Count; i++)
            if (CodecDescriptorByCodecId.TryGetValue(CodecTags[i].id, out encoder) && encoder.Properties.HasFlag(CodecPropFlags.TextSub))
                return CodecTags[i].id;

        return AVCodecID.None;
    }

    public static implicit operator AVOutputFormat*(MuxerSpec spec) => spec._ptr;

    public static MuxerSpec? FindMuxer(string fileName, string? mimeType = null) // maybe provide also with mime only?
    {
        AVOutputFormat* fmt;
        fmt = av_guess_format(null, fileName, mimeType);
        
        return fmt == null ? null : (MuxerSpec)FormatSpecByPtr[(nint)fmt];
    }

    // TBR: When CodecTags are not specified we might need to consider that is supported (eg. mpegts does not specify any codectags)
    public static List<MuxerSpec> FindMuxers(List<AVCodecID> codecIds, bool fileFormatsOnly = true)
    {
        List<MuxerSpec> muxers = [];

        for (int i = 0; i < MuxerSpecs.Count; i++)
        {
            var muxer = MuxerSpecs[i];

            if (muxer.Flags.HasFlag(MuxerSpecFlags.NoFile) && fileFormatsOnly)
                continue;

            if (muxer.Supports(codecIds))
                muxers.Add(muxer);
        }

        return muxers;
    }
}
