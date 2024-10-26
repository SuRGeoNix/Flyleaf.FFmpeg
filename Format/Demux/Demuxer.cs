namespace Flyleaf.FFmpeg.Format.Demux;

public unsafe class Demuxer : FormatContext
{
    #region Configuration Properties (RW)
    // E | D
    public StrictCompliance StrictCompliance            { get => _ptr->strict_std_compliance;            set => _ptr->strict_std_compliance = value; }
    public DemuxerFlags     Flags                       { get => (DemuxerFlags)_ptr->flags;              set => _ptr->flags = (FmtFlags2)value; }
    public FmtEventFlags    EventFlags                  { get => _ptr->event_flags;                      set => _ptr->event_flags = value; }

    // probe input format (set before avformat_open_input) - not required if we provide a format*
    public int              MaxDemuxerProbeBytes        { get => _ptr->format_probesize;                 set => _ptr->format_probesize = value; }
    public int              MaxStreams                  { get => _ptr->max_streams;                      set => _ptr->max_streams = value; }
    public long             SkipInitialBytes            { get => _ptr->skip_initial_bytes;               set => _ptr->skip_initial_bytes = value; }

    public string?          FormatWhitelist             { get => GetString(_ptr->format_whitelist);      set => _ptr->format_whitelist = av_strdup(value); }
    public string?          ProtocolWhitelist           { get => GetString(_ptr->protocol_whitelist);    set => _ptr->protocol_whitelist = av_strdup(value); } // if pb will be copied from it when null
    public string?          ProtocolBlacklist           { get => GetString(_ptr->protocol_blacklist);    set => _ptr->protocol_blacklist = av_strdup(value); } // if pb will be copied from it when null

    // probe stream info (set before avformat_find_stream_info) - not required if we dont Analyse()
    public string?          DecoderWhitelist            { get => GetString(_ptr->codec_whitelist);       set => _ptr->codec_whitelist = av_strdup(value); }
    public long             MaxDurationProbeBytes       { get => _ptr->duration_probesize;               set => _ptr->duration_probesize = value; }
    public int              MaxFPSProbeFrames           { get => _ptr->fps_probe_size;                   set => _ptr->fps_probe_size = value; }
    public long             MaxAnalyzeMcs               { get => _ptr->max_analyze_duration;             set => _ptr->max_analyze_duration = value; }
    public int              MaxProbePackets             { get => _ptr->max_probe_packets;                set => _ptr->max_probe_packets = value; }
    public int              MaxTsProbePackets           { get => _ptr->max_ts_probe;                     set => _ptr->max_ts_probe = value; }
    public long             MaxProbeBytes               { get => _ptr->probesize;                        set => _ptr->probesize = value; }
    public int              SkipEstimateDurationFromPts { get => _ptr->skip_estimate_duration_from_pts;  set => _ptr->skip_estimate_duration_from_pts = value; } // bool
    
    // Misc...
    public uint             MaxIndexSize                { get => _ptr->max_index_size;                   set => _ptr->max_index_size = value; }
    public uint             MaxPictureBuffer            { get => _ptr->max_picture_buffer;               set => _ptr->max_picture_buffer = value; }
    public int              MaxDemuxDelay               { get => _ptr->max_delay;                        set => _ptr->max_delay = value; }
    public int              Seek2Any                    { get => _ptr->seek2any;                         set => _ptr->seek2any = value; } // bool
    public int              UseWallClockTimestamps      { get => _ptr->use_wallclock_as_timestamps;      set => _ptr->use_wallclock_as_timestamps = value; } // bool
    
    public ErrorDetectFlags ErrorDetectFlags            { get => _ptr->error_recognition;                set => _ptr->error_recognition = value; } // bool
    public uint             CorrectTsOverflow           { get => _ptr->correct_ts_overflow;              set => _ptr->correct_ts_overflow = value; } // bool

    public IOFlags          IOFlags                     { get => _ptr->avio_flags;                       set => _ptr->avio_flags = value; }
    public AVCodecID        AudioDecoderId              { get => _ptr->audio_codec_id;                   set => _ptr->audio_codec_id =  value; }
    public AVCodecID        VideoDecoderId              { get => _ptr->video_codec_id;                   set => _ptr->video_codec_id =  value; }
    public AVCodecID        SubtitleDecoderId           { get => _ptr->subtitle_codec_id;                set => _ptr->subtitle_codec_id = value; }
    public AVCodecID        DataDecoderId               { get => _ptr->data_codec_id;                    set => _ptr->data_codec_id = value; }

    // TBR: getters from AVCodec* to CodecSpec, safe?
    public AudioDecoderSpec?AudioDecoder                { get => (AudioDecoderSpec?)FindCodec(_ptr->audio_codec); set => _ptr->audio_codec = value; }
    public VideoDecoderSpec?VideoDecoder                { get => (VideoDecoderSpec?)FindCodec(_ptr->video_codec); set => _ptr->video_codec = value; }
    public SubtitleDecoderSpec?
                            SubtitleDecoder             { get => (SubtitleDecoderSpec?)FindCodec(_ptr->subtitle_codec); set => _ptr->subtitle_codec = value; }
    //public AVCodec*         DataDecoder                 { get => ctx->data_codec;                       set => ctx->data_codec = value; } // There is no Data Codec!
    #endregion

    #region Properties (RO)
    public FFmpegClass      AVClass                     => FFmpegClass.Get(_ptr, D)!;
    public FFmpegClass?     AVClassPrivate              => FFmpegClass.Get(_ptr->priv_data, D);

    public string?          Url                         => GetString(_ptr->url);
    //public IOContext?       IOContext                   { get; } // Should expose? will be user's IO or current format's IO (can be change*>?)
    public DemuxerSpec      FormatSpec                  => (DemuxerSpec)FormatSpecByPtr[(nint)_ptr->iformat];
    public FmtCtxFlags      CtxFlags                    => _ptr->ctx_flags; // With HLS we need to remove Unseekable everytime* consider RW?
    public long             StartTimeMcs                => _ptr->start_time;
    public long             StartRealTimeMcs            => _ptr->start_time_realtime;
    public DateTime         StartRealTimeEpoch          => EPOCH.AddMicroseconds(_ptr->start_time_realtime);
    public long             DurationMcs                 => _ptr->duration;
    public long             BitRate                     => _ptr->bit_rate;
    public Dictionary<string, string>?
                            Metadata                    => AVDictToDict(_ptr->metadata); //{ get; private set; }
    public string?          MetadataGet(string key, DictReadFlags flags = DictReadFlags.None)
                                                        { var val = av_dict_get(_ptr->metadata, key, null, flags); return val != null ? GetString(val->value) : null; }
    public int              MetadataSet(string key, string value, DictWriteFlags flags = DictWriteFlags.None)
                                                        => av_dict_set(&_ptr->metadata, key, value, flags);
    public int              IORepositioned              => _ptr->io_repositioned;
    public int              ProbedScore                 => _ptr->probe_score;
    public AVDurationEstimationMethod
                            DurationEstimationMethod    => _ptr->duration_estimation_method;
    #endregion

    #region (Static) Exposing default IO Open/Close (might be used by custom IO Open/Close)
    public static AVFormatContext_io_open      IOOpenDefaultDlgt   { get;  private set; }
    public static AVFormatContext_io_close2    IOCloseDefaultDlgt  { get;  private set; }

    static Demuxer()
    {
        var tmpctx = avformat_alloc_context();
        IOOpenDefaultDlgt   = GetDelegateForFunctionPointer<AVFormatContext_io_open>(tmpctx->io_open.Pointer);
        IOCloseDefaultDlgt  = GetDelegateForFunctionPointer<AVFormatContext_io_close2>(tmpctx->io_close2.Pointer);
        avformat_free_context(tmpctx);
    }
    #endregion

    #region Constructor(s) / Open-Init
    public void*            InterruptOpaque             { get => _ptr->interrupt_callback.opaque; set => _ptr->interrupt_callback.opaque = value; } // TBR to avoid unsafe in the constructor (make it nint?)

    AVIOInterruptCB_callback?   InterruptDlgt;
    AVFormatContext_io_open?    IOOpenDlgt;
    AVFormatContext_io_close2?  IOCloseDlgt;
    
    // TBR: Check ReadPacket2
    //public event EventHandler? StreamsAdded;
    //public event EventHandler? MetadataUpdated;
    //public event EventHandler<MediaStream>? StreamMetadataUpdated;

    public Demuxer(AVFormatContext_io_open? ioopenClbk = null, AVFormatContext_io_close2? iocloseClbk = null) : base(avformat_alloc_context()) // constructor for avformat_open_input? not required*
    {
        Flags = DemuxerFlags.None; // wrongly sets AutoBsf by default (transfer to demuxer constructor*)
        
        if (ioopenClbk != null)
        {
            IOOpenDlgt = ioopenClbk;
            _ptr->io_open = IOOpenDlgt;
        }

        if (iocloseClbk != null)
        {
            IOCloseDlgt = iocloseClbk;
            _ptr->io_close2 = IOCloseDlgt;
        }
    }

    public FFmpegResult Open(IOContext ioContext, DemuxerSpec? spec = null, Dictionary<string, string>? opts = null)
        => Open1(ioContext: ioContext, inFmt: spec ?? (AVInputFormat*)null, opts: opts);

    public FFmpegResult Open(string url, DemuxerSpec? spec = null, Dictionary<string, string>? opts = null, AVIOInterruptCB_callback? interruptClbk = null)//, void* interruptClbkOpaque = null) will require callers to use unsafe
        => Open1(url: url, inFmt: spec ?? (AVInputFormat*)null, opts: opts, interruptClbk: interruptClbk, interruptClbkOpaque: null);

    // TBR: use just NOFILE fmt without url? (check at least not null*)
    public FFmpegResult Open(DemuxerSpec spec, Dictionary<string, string>? opts = null, AVIOInterruptCB_callback? interruptClbk = null, void* interruptClbkOpaque = null)
        => Open1(inFmt: spec, opts: opts, interruptClbk: interruptClbk, interruptClbkOpaque: interruptClbkOpaque);
    
    int checkOpens;
    FFmpegResult Open1(IOContext? ioContext = null, string? url = null, AVInputFormat* inFmt = null, Dictionary<string, string>? opts = null, AVIOInterruptCB_callback? interruptClbk = null, void* interruptClbkOpaque = null)
    {
        if (checkOpens++ > 0)
            throw new Exception("Demuxer cannot be re-used");

        InterruptDlgt = interruptClbk;

        if (ioContext != null)
        {
            _ptr->pb                = ioContext._ptr;
            InterruptDlgt           = ioContext.InterruptDlgt;
            _ptr->interrupt_callback= ioContext.int_cb;
        }
        else
        {
            _ptr->interrupt_callback.opaque  = interruptClbkOpaque;
            _ptr->interrupt_callback.callback= InterruptDlgt;
        }

        FFmpegResult ret = opts != null ? Open12(opts, url, inFmt) : Open11(url, inFmt); // possible to interrupt/cancel and still return 0?
        ret.ThrowOnFailure();

        if (!Disposed)
            FillAll();

        return ret;
    }

    FFmpegResult Open11(string? url = null, AVInputFormat* inFmt = null)
    {
        fixed(AVFormatContext** ptrPtr = &_ptr)
            return new(avformat_open_input(ptrPtr, url, inFmt, null));
    }

    FFmpegResult Open12(Dictionary<string, string> opts, string? url = null, AVInputFormat* inFmt = null)
    {
        var avopts  = AVDictFromDict(opts);

        FFmpegResult ret;
        fixed(AVFormatContext** ptrPtr = &_ptr)
             ret = new(avformat_open_input(ptrPtr, url, inFmt, &avopts)); // We can pass ctx->iformat manually before here (not the same for url/filename)
        
        opts.Clear();

        if (avopts != null)
        {
            AVDictToDict(opts, avopts);
            AVDictFree(&avopts);
        }

        return ret;
    }
    #endregion

    #region Fill Streams / Chapters / Programs / StreamGroups (Expose to check manually if required)
    public void FillAll() // first time if we don't care about events (return false/true)
    {
        FillStreams(); // must be first
        FillChapters();
        FillPrograms();
        FillStreamGroups();
    }

    public bool FillStreams()
    {
        if (streams.Count >= _ptr->nb_streams)
            return false;

        int i = streams.Count;
        for (; i < _ptr->nb_streams; i++)
        {
            var stream = MediaStream.Get(this, _ptr->streams[i]);
            stream._ptr->discard = AVDiscard.All; // disabled by default
            streams.Add(stream);
        }

        return true;
    }

    public bool FillChapters()
    {
        if (chapters.Count >= _ptr->nb_chapters)
            return false;

        int i = chapters.Count;
        for (; i < _ptr->nb_chapters; i++)
        {
            var chapter = new MediaChapter(this, _ptr->chapters[i]);
            chapters.Add(chapter);
        }

        return true;
    }

    public bool FillPrograms()
    {
        if (programs.Count >= _ptr->nb_programs)
            return false;

        int i = programs.Count;
        for (; i < _ptr->nb_programs; i++)
        {
            var program = new MediaProgram(this, _ptr->programs[i]);
            bool needed = false;
            for (int j = 0; j < _ptr->programs[i]->nb_stream_indexes; j++)
            {
                var stream = streams[(int)_ptr->programs[i]->stream_index[j]];
                if (!needed)
                    needed = stream._ptr->discard < AVDiscard.All;
                program.streams.Add(stream);
                stream.programs.Add(program);
            }
            program._ptr->discard = needed ? AVDiscard.Default : AVDiscard.All; // disabled by default except if stream already enabled
            programs.Add(program);
        }
        
        return true;
    }

    public bool FillStreamGroups()
    {
        if (streamGroups.Count >= _ptr->nb_stream_groups)
            return false;

        int i = streamGroups.Count;
        for (; i < _ptr->nb_stream_groups; i++)
        {
            var streamGroup = new StreamGroup(this, _ptr->stream_groups[i]); 
            for (int j = 0; j < _ptr->stream_groups[i]->nb_streams; j++)
            {
                var stream = streams[_ptr->stream_groups[i]->streams[j]->index];
                streamGroup.streams.Add(stream);
                stream.streamGroups.Add(streamGroup);
            }
            streamGroups.Add(streamGroup);
        }

        return true;
    }
    #endregion

    #region Streams / Programs Activation
    public bool IsEnabled(MediaStream stream)
        => stream._ptr->discard < AVDiscard.All;

    public bool IsEnabled(MediaProgram program)
        => program._ptr->discard < AVDiscard.All;

    public void Enable(MediaStream stream, AVDiscard discard = AVDiscard.Default)
    {
        // Enable stream
        stream._ptr->discard = discard;

        // Enables stream's first program if none of it's programs already enabled
        // TBR: this should also check to transfer other streams to this program if possible and disable the other programs (we consider that the user will first enable the 'single program' stream)
        // Possible let user force which program to use for the stream / Also if we enable/disable multiple streams at once we can select the best combination*
        if (stream.programs.Count > 0 && 
            stream.programs.All(p => p._ptr->discard == AVDiscard.All))
            stream.programs[0]._ptr->discard = AVDiscard.Default;            
    }

    public void Disable(MediaStream stream)
    {
        // Disable stream
        stream._ptr->discard = AVDiscard.All;

        // Disable stream's programs if all of their streams are disabled
        // TBR: we could re-evaluate the programs here in case the rest streams can now move to another enabled program
        stream.programs.
            Where(p => p.streams.All(s => s._ptr->discard == AVDiscard.All)).ToList().
            ForEach(p => p._ptr->discard = AVDiscard.All);
    }
    #endregion

    public FFmpegResult Analyse() //Dictionary<string, string>? opts = null) | https://github.com/FFmpeg/FFmpeg/blob/7dabad079b783e921747de96597ea47cab244a11/fftools/cmdutils.c#L1055
    {
        /* TBR
         * This calls read_frame_internal which checks for st->discard < AVDISCARD_ALL which means that it will not proper analyse if we already have the steams disabled
         *  (That might help to 'exclude' streams from anlayse? however this probably force it read until max_analyze_duration limit which might be worse)
         *  
         * Currently we enable all streams if there were all disabled and then disable them back
         */

        bool reDisableStreams = false;
        if (reDisableStreams = streams.All(s => !IsEnabled(s)))
            streams.ForEach(s => Enable(s));

        //var avopts = AVDictFromDict(opts);
        FFmpegResult ret = new(avformat_find_stream_info(_ptr, null)); // TBR: Expects array otherwise access violation!

        //if (avopts != null)
        //{
        //    AVDictToDict(opts!, avopts);
        //    AVDictFree(&avopts);
        //}

        FillAll();

        if (reDisableStreams)
            streams.ForEach(s => Disable(s));

        return ret;
    }

    public FFmpegResult Flush()
        => new(avformat_flush(_ptr)); // can muxer do that?

    // deprecated (already exist in codec params?* use codec to context etc)
    //public void InjectGlobalSideData()
        //=> av_format_inject_global_side_data(_ptr); // during read_frame_internal will pass stream's global side data to packets

    public FFmpegResult Seek(long ts, int streamIndex = -1, SeekFlags flags = SeekFlags.None)
        => new(av_seek_frame(_ptr, streamIndex, ts, flags));

    public FFmpegResult SeekFile(long ts, int streamIndex = -1, SeekFlags flags = SeekFlags.None, long minTs = long.MinValue, long maxTs = long.MaxValue)
        => new(avformat_seek_file(_ptr, streamIndex, minTs, ts, maxTs, flags));

    public FFmpegResult ReadPacket(PacketBase pkt)
        => ReadPacket(pkt._ptr);

    public FFmpegResult ReadPacket(AVPacket* pkt)
        => new(av_read_frame(_ptr, pkt));

    // TBR: Probably this should be handlled from the caller
    //public FFmpegResult ReadPacket2(PacketBase pkt)
    //    => ReadPacket2(pkt._ptr);

    //// + StreamAdded / Packet's Stream / Excludes inactive streams / Checks Formats & Streams Metadata updated event
    //public FFmpegResult ReadPacket2(AVPacket* pkt)
    //{
    //    FFmpegResult ret = new(av_read_frame(_ptr, pkt));
        
    //    if (ret.Failed)
    //        return ret; // ret enum?

    //    // Check for new streams (maybe only for specific formats* - no header?)
    //    if (_ptr->nb_streams > streams.Count)
    //    {
    //        FillAll();
    //        StreamsAdded?.Invoke(this, new()); // possible the current packet / stream could become inactive from here and we should avoid returning it*
    //    }

    //    var stream = _ptr->streams[pkt->stream_index];

    //    // Ensure is active stream (Let higher layer to handle enable/disable streams/programs) we just care about ignoring non-active here
    //    if (stream->discard == AVDiscard.All)
    //        return ReadPacket2(pkt); // WARN: this will cause infinite loop if we don't have any active streams

    //    // Metadata Updated (Format)
    //    if (EventFlags.HasFlag(FmtEventFlags.MetadataUpdated))
    //    {
    //        EventFlags &= ~FmtEventFlags.MetadataUpdated;
    //        MetadataUpdated?.Invoke(this, new());
    //    }

    //    // Metadata Updated (Stream)
    //    if (stream->event_flags.HasFlag(StreamEventFlags.MetadataUpdated))
    //    {
    //        stream->event_flags &= ~StreamEventFlags.MetadataUpdated;
    //        StreamMetadataUpdated?.Invoke(this, streams[pkt->stream_index]);
    //    }

    //    return ret;
    //}

    public FFmpegResult ReadPlay() // only for RTSP (required?)
        => new(av_read_play(_ptr));

    public FFmpegResult ReadPause() // only for RTSP (required?)
        => new(av_read_pause(_ptr));

    // should go to demuxed live stream?
    public FFmpegResult SearchTimestamp(long ts, AVStream* stream, SeekFlags flags = SeekFlags.None)
        => new(av_index_search_timestamp(stream, ts, flags));

    protected override void Close()
    {
        fixed(AVFormatContext** ptrPtr = &_ptr)
            avformat_close_input(ptrPtr);
    }
}
