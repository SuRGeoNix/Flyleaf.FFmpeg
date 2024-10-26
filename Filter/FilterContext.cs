namespace Flyleaf.FFmpeg.Filter;

public unsafe class FilterContext
{
    // RW?
    public int                  HWExtraFrames       { get => _ptr->extra_hw_frames; set => _ptr->extra_hw_frames = value; }
    public HWDeviceContextBase? HWDeviceContext     { get => _ptr->hw_device_ctx == null ? null : new HWDeviceContextView(_ptr->hw_device_ctx); set { if (_ptr->hw_device_ctx != null || value == null) return; if (!Filter.Flags.HasFlag(FilterFlags.Hwdevice)) throw new Exception("Filter does not support hw device"); _ptr->hw_device_ctx = value.RefRaw(); } } // no owner / don't overwrite (force only after getformat)
    public FilterThreadFlags    ThreadFlags         { get => _ptr->thread_type;     set => _ptr->thread_type = value; }
    public int                  Threads             { get => _ptr->nb_threads;      set => _ptr->nb_threads = value; }

    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr)!;
    public FFmpegClass?         AVClassPrivate      => FFmpegClass.Get(_ptr->priv);
    public FilterGraph          FilterGraph         { get; }
    public FilterSpec           Filter              { get; }

    public string?              Name                => GetString(_ptr->name);
    public uint                 Ready               => _ptr->ready;

    // inputs / outputs links under inpads/outpads
    public ReadOnlyCollection<FilterPadIn>
                                InPads              { get; }
    public ReadOnlyCollection<FilterPadOut>
                                OutPads             { get; }

    List<FilterPadIn>   inPads = [];
    List<FilterPadOut>  outPads = [];
    Dictionary<nint, FilterPad> padPtrToPad = [];

    public readonly AVFilterContext* _ptr;

    public static implicit operator AVFilterContext*(FilterContext filterCtx)
        => filterCtx._ptr;

    public static FilterContext Get(FilterGraph graph, AVFilterContext* ctx)
    {
        var filtName = GetString(ctx->filter->name)!;

        return filtName switch
        {
            "buffer"        => new VideoBufferSource(graph, ctx),
            "abuffer"       => new AudioBufferSource(graph, ctx),
            "buffersink"    => new VideoBufferSink(graph, ctx),
            "abuffersink"   => new AudioBufferSink(graph, ctx),
            _               => new FilterContext(graph, ctx),
        };
    }

    internal FilterContext(FilterGraph graph, AVFilterContext* ctx) : this(graph, new(ctx->filter), null, null, false, ctx) { }
    public FilterContext(FilterGraph graph, string filterName, string? name = null, string? args = null) : this(graph, FindFilter(filterName), name, args, true) { }
    public FilterContext(FilterGraph graph, string filterName, bool initialize, string? name = null) : this(graph, FindFilter(filterName), name, null, initialize) { }
    public FilterContext(FilterGraph graph, FilterSpec filter, string? name = null, string? args = null) : this(graph, filter, name, args, true) { }
    public FilterContext(FilterGraph graph, FilterSpec filter, bool initialize, string? name = null) : this(graph, filter, name, null, initialize) { }

    FilterContext(FilterGraph graph, FilterSpec? filter, string? name = null, string? args = null, bool initialize = true, AVFilterContext* existingCtx = null)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        FilterGraph = graph;
        Filter      = filter;
        
        if (existingCtx != null)
            _ptr = existingCtx;
        else if (initialize)
        {
            fixed(AVFilterContext** ptrPtr = &_ptr)
                if (avfilter_graph_create_filter(ptrPtr, Filter, name, args, null, graph) < 0)
                    throw new Exception($"Filter {filter.Name} creation failed");
        }
        else if ((_ptr = avfilter_graph_alloc_filter(graph, Filter, name)) == null)
                throw new Exception($"Filter {filter.Name} allocation failed");

        InPads      = new(inPads);
        OutPads     = new(outPads);
        
        for(int i = 0; i < _ptr->nb_inputs; i++)
            inPads.Add(new(this, _ptr->input_pads, i));
        
        for(int i = 0; i < _ptr->nb_outputs; i++)
            outPads.Add(new(this, _ptr->output_pads, i));

        if (existingCtx == null)
            graph.AddFilterCtx(this);
    }

    public int InitFilter(Dictionary<string, string> opts)
    {
        var avopts  = AVDictFromDict(opts);
        int ret = avfilter_init_dict(_ptr, ref avopts);

        opts.Clear();

        if (avopts != null)
        {
            AVDictToDict(opts, avopts);
            AVDictFree(&avopts);
        }

        return ret;
    }

    public int InitFilter(string? args = null)
        => avfilter_init_str(_ptr, args);

    public FilterContext Link(FilterContext filterCtx)
    {
        outPads[0].Link(filterCtx.inPads[0]);
        return filterCtx; // so we can continue using Link as a chain
    }

    public void Link(FilterPadIn inPad)
        => outPads[0].Link(inPad);

    static System.Reflection.FieldInfo ptrField = typeof(FilterContext).GetField(nameof(_ptr))!;
    internal void Free()
    {
        // FilterGraph owns it (free it only if manually removed)
        if (_ptr == null)
            return;

        avfilter_free(_ptr); 
        ptrField.SetValue(this, null);
    }
}
