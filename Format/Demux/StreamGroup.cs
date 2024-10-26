using System.Collections.ObjectModel;

namespace Flyleaf.FFmpeg.Format.Demux;

public unsafe class StreamGroup
{
    public DispositionFlags             Disposition     => _ptr->disposition;
    public long                         Id              => _ptr->id;
    public uint                         Index           => _ptr->index;
    public Dictionary<string, string>?  Metadata        => AVDictToDict(_ptr->metadata); //{ get; private set; } // once? (no eventflags here/update in parallel with streams*)
    public string?                      MetadataGet(string key, DictReadFlags flags = DictReadFlags.None)
                                                        { var val = av_dict_get(_ptr->metadata, key, null, flags); return val != null ? GetString(val->value) : null; }
    public AVStreamGroup_params         Params          => _ptr->@params; // structs incomplete (tbr)
    public AVStreamGroupParamsType      Type            => _ptr->type;

    public ReadOnlyCollection<MediaStream>  Streams     = null!;
    internal List<MediaStream> streams = [];

    public Mux.Muxer?                   Muxer           { get; private set; }
    public Demuxer?                     Demuxer         { get; private set; }

    public readonly AVStreamGroup* _ptr;

    internal StreamGroup(FormatContext fmt, AVStreamGroup* group)
    {
        if (fmt is Muxer muxer)
            Muxer = muxer;
        else
            Demuxer = (Demuxer) fmt;

        Streams     = new(streams);
        _ptr = group;
    }

    //public bool FillMetadata()
    //{
    //    var newMetadata = AVDictToDict(group->metadata);
        
    //    // check only count diff
    //    if ((newMetadata == null && Metadata != null) || (newMetadata != null && Metadata == null) || (newMetadata != null && Metadata != null && newMetadata.Count != Metadata.Count))
    //    {
    //        Metadata = newMetadata;
    //        return true;
    //    }

    //    return false;
    //}
}
