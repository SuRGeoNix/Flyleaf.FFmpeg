namespace Flyleaf.FFmpeg.Format.Demux;

public unsafe class MediaProgram
{
    public int          Id              => _ptr->id;
    public int          Number          => _ptr->program_num;
    public int          PcrPid          => _ptr->pcr_pid;
    public int          PmtPid          => _ptr->pmt_pid;
    public int          PmtVer          => _ptr->pmt_version;
    public Dictionary<string, string>? 
                        Metadata        => AVDictToDict(_ptr->metadata); //{ get; private set; } // once? (no eventflags here/update in parallel with streams*)
    public string?      MetadataGet(string key, DictReadFlags flags = DictReadFlags.None)
                                        { var val = av_dict_get(_ptr->metadata, key, null, flags); return val != null ? GetString(val->value) : null; }

    //helpers
    public bool         Enabled         => _ptr->discard != AVDiscard.All;

    public ReadOnlyCollection<MediaStream>  Streams     = null!;
    internal List<MediaStream> streams = [];

    public Demuxer?     Demuxer         { get; private set; }
    public Mux.Muxer?   Muxer           { get; private set; }
    
    public readonly AVProgram* _ptr;

    public MediaProgram(FormatContext fmt, AVProgram* prog)
    {
        if (fmt is Mux.Muxer muxer)
            Muxer = muxer;
        else
            Demuxer = (Demuxer) fmt;

        Streams     = new(streams);
        _ptr   = prog;
    }

    //public bool FillMetadata()
    //{
    //    var newMetadata = AVDictToDict(prog->metadata);

    //    // check only count diff
    //    if ((newMetadata == null && Metadata != null) || (newMetadata != null && Metadata == null) || (newMetadata != null && Metadata != null && newMetadata.Count != Metadata.Count))
    //    {
    //        Metadata = newMetadata;
    //        return true;
    //    }

    //    return false;
    //}
}
