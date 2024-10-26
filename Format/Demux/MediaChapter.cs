using System.IO;

namespace Flyleaf.FFmpeg.Format.Demux;

public unsafe class MediaChapter
{
    public long                         StartTime       => _ptr->start;
    public long                         EndTime         => _ptr->end;
    public Dictionary<string, string>?  Metadata        => AVDictToDict(_ptr->metadata); //{ get; private set; }
    public string?                      MetadataGet(string key, DictReadFlags flags = DictReadFlags.None)
                                                        { var val = av_dict_get(_ptr->metadata, key, null, flags); return val != null ? GetString(val->value) : null; }
    public AVRational                   Timebase        => _ptr->time_base;

    public Demuxer?                     Demuxer         { get; private set; }
    public Mux.Muxer?                   Muxer           { get; private set; }

    public readonly AVChapter* _ptr;

    internal MediaChapter(FormatContext fmt, AVChapter* chapter)
    {
         if (fmt is Mux.Muxer muxer)
            Muxer = muxer;
        else
            Demuxer = (Demuxer) fmt;

        _ptr = chapter;
    }

    //public bool FillMetadata()
    //{
    //    var newMetadata = AVDictToDict(chapter->metadata);

    //    // check only count diff
    //    if ((newMetadata == null && Metadata != null) || (newMetadata != null && Metadata == null) || (newMetadata != null && Metadata != null && newMetadata.Count != Metadata.Count))
    //    {
    //        Metadata = newMetadata;
    //        return true;
    //    }

    //    return false;
    //}
}

// mux?