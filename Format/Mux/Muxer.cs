namespace Flyleaf.FFmpeg.Format.Mux;

public unsafe class Muxer : FormatContext
{
    #region Configuration Properties (RW)
    public StrictCompliance StrictCompliance        { get => _ptr->strict_std_compliance;    set => _ptr->strict_std_compliance = value; }
    public MuxerFlags       Flags                   { get => (MuxerFlags)_ptr->flags;        set => _ptr->flags = (FmtFlags2)value; }
    public FmtEventFlags    EventFlags              { get => _ptr->event_flags;              set => _ptr->event_flags = value; }

    public int              AudioPreload            { get => _ptr->audio_preload;            set => _ptr->audio_preload = value; }
    public AvoidNegTSFlags  AvoidNegTSFlags         { get => _ptr->avoid_negative_ts;        set => _ptr->avoid_negative_ts = value; }
    public int              FlushPackets            { get => _ptr->flush_packets;            set => _ptr->flush_packets = value; }
    public int              MetadataHeaderPadding   { get => _ptr->metadata_header_padding;  set => _ptr->metadata_header_padding = value; }
    public int              MaxChunkDuration        { get => _ptr->max_chunk_duration;       set => _ptr->max_chunk_duration = value; }
    public int              MaxChunkSize            { get => _ptr->max_chunk_size;           set => _ptr->max_chunk_size = value; }
    public long             MaxInterleaveDelta      { get => _ptr->max_interleave_delta;     set => _ptr->max_interleave_delta = value; }
    public int              MaxMuxDelay             { get => _ptr->max_delay;                set => _ptr->max_delay = value; }
    public long             OutputTSOffset          { get => _ptr->output_ts_offset;         set => _ptr->output_ts_offset = value; }
    public uint             PacketSize              { get => _ptr->packet_size;              set => _ptr->packet_size = value; }
    #endregion

    #region Properties (RO) *TBR
    public FFmpegClass      AVClass                 => FFmpegClass.Get(_ptr, E)!;
    public FFmpegClass?     AVClassPrivate          => FFmpegClass.Get(_ptr->priv_data, E);
    // Should be RO?*
    public string?          Url                     { get => GetString(_ptr->url);           set => AVStrDupReplace(value, &_ptr->url); }
    public IOContext?       IOContext               { get; }
    public MuxerSpec        MuxerSpec               => (MuxerSpec)FormatSpecByPtr[(nint)_ptr->oformat]; // WARN: Never set ->i/oformat directly from AV* as it is actually an FF* and requires to allocate also priv_data and set default options (ffmpeg examples do it anyways?*)
    public long             StartTimeMcs            => _ptr->start_time;
    public long             StartRealTime           { get => _ptr->start_time_realtime;      set => _ptr->start_time_realtime = value; }
    public long             Duration                { get => _ptr->duration;                 set => _ptr->duration = value; }
    public long             BitRate                 { get => _ptr->bit_rate;                 set => _ptr->bit_rate = value; }
    public Dictionary<string, string>?
                            Metadata                { get => AVDictToDict(_ptr->metadata);   set => AVDictReplaceFromDict(value, &_ptr->metadata); }
    public string?          MetadataGet(string key, DictReadFlags flags = DictReadFlags.None)
                                                    { var val = av_dict_get(_ptr->metadata, key, null, flags); return val != null ? GetString(val->value) : null; }
    #endregion

    // Required oformat, opened pb if NOFILE, at least one stream if NOSTREAMS + stream requirements
    // TBR: Constructor to just allocate format and manually set oformat, url etc... | https://ffmpeg.org/doxygen/trunk/group__lavf__encoding.html | https://ffmpeg.org/doxygen/trunk/transcode_aac_8c-example.html#a26
    public Muxer(MuxerSpec spec, IOContext? ioContext, bool disposeIOContext = true) : this(spec, null, null, ioContext, disposeIOContext) { }
    public Muxer(MuxerSpec spec, string fileName) : this(spec, null, fileName) { }

    bool disposeIOContext;
    internal Muxer(AVOutputFormat* outFmt = null, string? outFmtName = null, string? fileName = null, IOContext? ioContext = null, bool disposeIOContext = true) : base()
    {
        fixed(AVFormatContext** ptrPtr = &_ptr)
            new FFmpegResult(avformat_alloc_output_context2(ptrPtr, outFmt, outFmtName, fileName)).ThrowOnFailure(); // this just allocates + av_guess_format and sets the default opts of the oformat

        if (ioContext == null)
        {
            if (!MuxerSpec.Flags.HasFlag(MuxerSpecFlags.NoFile))
            {
                ArgumentNullException.ThrowIfNull(fileName);

                try
                {
                    IOContext = new(fileName, IOFlags.Write);
                } catch (FFmpegException) { Dispose(); throw; }
                
                _ptr->pb = IOContext;
                this.disposeIOContext = true;
            }
        }
        else
        {
            IOContext = ioContext;
            this.disposeIOContext = disposeIOContext;
        }
    }

    // TBR: Check auto dispose in case of init/write header failure
    public FFmpegResult InitOutput()
        => new(avformat_init_output(_ptr, null));

    public FFmpegResult InitOutput(Dictionary<string, string> opts) // if use this don't pass same opts to WriteHeader (returns AVSTREAM_INIT_IN_WRITE_HEADER / AVSTREAM_INIT_IN_INIT_OUTPUT on success)
    {
        var avopts          = AVDictFromDict(opts);
        FFmpegResult ret    = new(avformat_init_output(_ptr, & avopts));

        opts.Clear();

        if (avopts != null)
        {
            AVDictToDict(opts, avopts);
            AVDictFree(&avopts);
        }

        return ret;
    }

    public FFmpegResult WriteHeader(Dictionary<string, string>? opts = null) // will InitOutput if not already initialized
    {
        var avopts = AVDictFromDict(opts);

        if (_ptr->metadata != null) // TBR
            _= av_dict_set(ref avopts, "movflags", "use_metadata_tags", DictWriteFlags.DontOverwrite);

        FFmpegResult ret = new(avformat_write_header(_ptr, &avopts)); // free/check rest

        if (avopts != null)
        {
            if (opts != null)
                AVDictToDict(opts, avopts);
            AVDictFree(&avopts);
        }

        return ret;
    }

    // TODO: Packet/Frame probably we have more info (eg. streamIndex possible also rescale*)
    public FFmpegResult WriteTrailer()
        => new(av_write_trailer(_ptr));

    public FFmpegResult WritePacket(PacketBase pkt)
        => new(av_write_frame(_ptr, pkt));

    public FFmpegResult WritePacket(AVPacket* pkt)
        => new(av_write_frame(_ptr, pkt));

    public FFmpegResult WritePacketInterleaved(PacketBase pkt)
        => new(av_interleaved_write_frame(_ptr, pkt));

    public FFmpegResult WritePacketInterleaved(AVPacket* pkt)
        => new(av_interleaved_write_frame(_ptr, pkt));

    public FFmpegResult WriteUncodedFrame(FrameBase frm, int streamIndex)
        => new(av_write_uncoded_frame(_ptr, streamIndex, frm));

    public FFmpegResult WriteUncodedFrame(AVFrame* frm, int streamIndex)
        => new(av_write_uncoded_frame(_ptr, streamIndex, frm));

    public FFmpegResult WriteUncodedFrameQuery(int streamIndex)
        => new(av_write_uncoded_frame_query(_ptr, streamIndex));

    public (bool success, long dts, long wall) GetOutputTimestamp(int streamIndex)
    {
        long dts, wall;
        bool success = av_get_output_timestamp(_ptr, streamIndex, &dts, &wall) == 0;
        return (success, dts, wall);
    }

    internal AVStream* NewStream(AVMediaType type) 
    {
        var stream = avformat_new_stream(_ptr, null);
        if (stream == null)
            return null;
        stream->codecpar->codec_type = type;
        streams.Add(MediaStream.Get(this, stream));
        return stream;
    }
    
    internal AVProgram* NewProgram(int id)
    {
        var prog = av_new_program(_ptr, id); // if id exists returns the existing program
        if (prog == null)
            return null;

        programs.Add(new(this, prog));
        return prog;
    }
    
    internal AVStreamGroup* NewStreamGroup(AVStreamGroupParamsType type, Dictionary<string, string>? opts = null)
    {
        var avopts = AVDictFromDict(opts);
        var group = avformat_stream_group_create(_ptr, type, &avopts);
        if (group == null)
            return null;

        streamGroups.Add(new(this, group));
        return group;
    }

    internal void AddStreamToProgram(AVStream* stream, AVProgram* prog)
    {
        Demux.MediaProgram? existing = null;
        for (int i = 0; i < programs.Count; i++)
            if (programs[i].Id == prog->id)
                { existing = programs[i]; break; }

        if (existing == null)
            return;

        av_program_add_stream_index(_ptr, prog->id, (uint)stream->index);
        existing.streams.Add(streams[stream->index]);
        streams[stream->index].programs.Add(existing);
    }

    internal void AddStreamToStreamGroup(AVStream* stream, AVStreamGroup* group)
    {
        Demux.StreamGroup? existing = null;
        for (int i = 0; i < streamGroups.Count; i++)
            if (streamGroups[i]._ptr == group)
                { existing = streamGroups[i]; break; }

        if (existing == null)
            return;

        _ = avformat_stream_group_add_stream(group, stream);
        existing.streams.Add(streams[stream->index]);
        streams[stream->index].streamGroups.Add(existing);
    }

    protected override void Close()
    {
        avformat_free_context(_ptr); 
        ptrField.SetValue(this, null);

        if (disposeIOContext)
            IOContext?.Dispose();
    }
}
