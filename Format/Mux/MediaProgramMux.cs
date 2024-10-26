namespace Flyleaf.FFmpeg.Format.Mux;

public unsafe class MediaProgramMux
{
    public int          Id              { get => _ptr->id;                      set => _ptr->id = value; }
    public int          Number          { get => _ptr->program_num;             set => _ptr->program_num = value; }
    public int          PcrPid          { get => _ptr->pcr_pid;                 set => _ptr->pcr_pid = value; }
    public int          PmtPid          { get => _ptr->pmt_pid;                 set => _ptr->pmt_pid = value; }
    public int          PmtVer          { get => _ptr->pmt_version;             set => _ptr->pmt_version = value; }
    //public AVDiscard    Discard         { get => prog->discard;                 set => prog->discard = value; } // Demux only?
    public Dictionary<string, string>?
                        Metadata        { get => AVDictToDict(_ptr->metadata);  set => AVDictReplaceFromDict(value, &_ptr->metadata); }
    public string?      MetadataGet(string key, DictReadFlags flags = DictReadFlags.None)
                                        { var val = av_dict_get(_ptr->metadata, key, null, flags); return val != null ? GetString(val->value) : null; }
    public int          MetadataSet(string key, string value, DictWriteFlags flags = DictWriteFlags.None)
                                        => av_dict_set(&_ptr->metadata, key, value, flags);

    public Muxer        Muxer           { get; private set; }
    
    public readonly AVProgram* _ptr;

    public MediaProgramMux(Muxer muxer, int progId) // Handle error? (maybe it should be in the format?)
    {
        Muxer   = muxer;
        _ptr    = muxer.NewProgram(progId);
    }

    public void AddStream(MediaStreamMux stream)
        => Muxer.AddStreamToProgram(stream._ptr, _ptr);
}
