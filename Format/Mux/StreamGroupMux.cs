namespace Flyleaf.FFmpeg.Format.Mux;

public unsafe class StreamGroupMux
{
    public DispositionFlags             Disposition     { get => _ptr->disposition; set => _ptr->disposition = value; }
    public long                         Id              { get => _ptr->id; set => _ptr->id = value; }
    public uint                         Index           => _ptr->index;
    public Dictionary<string, string>?  Metadata        { get => AVDictToDict(_ptr->metadata); set => AVDictReplaceFromDict(value, &_ptr->metadata); }
    public string?                      MetadataGet(string key, DictReadFlags flags = DictReadFlags.None)
                                                        { var val = av_dict_get(_ptr->metadata, key, null, flags); return val != null ? GetString(val->value) : null; }
    public int                          MetadataSet(string key, string value, DictWriteFlags flags = DictWriteFlags.None)
                                                        => av_dict_set(&_ptr->metadata, key, value, flags);
    public AVStreamGroup_params         Params          => _ptr->@params; // structs incomplete (tbr)
    public AVStreamGroupParamsType      Type            => _ptr->type;

    public Muxer                        Muxer           { get; private set; }
    
    public readonly AVStreamGroup* _ptr;
    
    public StreamGroupMux(Muxer muxer, AVStreamGroupParamsType type, Dictionary<string, string>? opts = null)
    {
        Muxer = muxer;
        _ptr = muxer.NewStreamGroup(type, opts);
    }

    public void AddStream(MediaStreamMux stream)
        => Muxer.AddStreamToStreamGroup(stream._ptr, _ptr);
}
