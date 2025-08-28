namespace Flyleaf.FFmpeg.Filter;

public unsafe class FilterGraph : IDisposable
{
    public FFmpegClass          AVClass                 => FFmpegClass.Get(_ptr)!;
    public FilterThreadFlags    ThreadFlags             { get => _ptr->thread_type;                     set => AVClass.Set("thread_type",       value); }
    public int                  Threads                 { get => _ptr->nb_threads;                      set => AVClass.Set("threads",           value); }
    public string?              ImageConvOpts           { get => GetString(_ptr->scale_sws_opts);       set => AVClass.Set("scale_sws_opts",    value); }   // Video only
    public string?              SampleConvOpts          { get => GetString(_ptr->aresample_swr_opts);   set => AVClass.Set("aresample_swr_opts",value); }   // Audio only
    public uint                 MaxBufferedFrames       { get => _ptr->max_buffered_frames;             set => _ptr->max_buffered_frames = value;}

    public ReadOnlyCollection<FilterContext>
                                Filters                 { get; }
    List<FilterContext>     filters = [];
    Dictionary<nint, int>   filterCtxPtrToIndex = []; // filters do not have indexes

    public bool                 Disposed                => _ptr == null;
    public readonly AVFilterGraph* _ptr;
    public static implicit operator AVFilterGraph*(FilterGraph graph)
        => graph._ptr;
    
    public FilterGraph()
    {
        _ptr = avfilter_graph_alloc();
        Filters = new(filters);
    }

    public FFmpegResult Config()
    {
        FFmpegResult ret = new(avfilter_graph_config(_ptr, null));
        if (ret.Success)
            RefreshFilters();

        return ret;
    }

    public FFmpegResult Parse(string filters, out List<FilterPadIn> inPadOrphan, out List<FilterPadOut> outPadOrphan, bool useParse2 = false)
    {
        inPadOrphan = [];
        outPadOrphan = [];

        AVFilterInOut* inputs = null;
        AVFilterInOut* outputs = null;
        FFmpegResult ret;

        if (!useParse2)
            ret = new(avfilter_graph_parse_ptr(_ptr, filters, &inputs, &outputs, null));
        else
            ret = new(avfilter_graph_parse2(_ptr, filters, &inputs, &outputs));

        if (ret.Failed)
        {
            avfilter_inout_free(ref inputs);
            avfilter_inout_free(ref outputs);
            return ret;
        }

        RefreshFilters();

        AVFilterInOut* cur = inputs;
        while (cur != null)
        {
            if (!filterCtxPtrToIndex.TryGetValue((nint)cur->filter_ctx, out int filterIndex))
                throw new Exception("Couldn't map filter_ctx to filters");

            inPadOrphan.Add(this.filters[filterIndex].InPads[cur->pad_idx]);
            cur = cur->next;
        }

        cur = outputs;
        while (cur != null)
        {
            if (!filterCtxPtrToIndex.TryGetValue((nint)cur->filter_ctx, out int filterIndex))
                throw new Exception("Couldn't map filter_ctx to filters");

            outPadOrphan.Add(this.filters[filterIndex].OutPads[cur->pad_idx]);
            cur = cur->next;
        }

        avfilter_inout_free(ref inputs);
        avfilter_inout_free(ref outputs);

        return ret;
    }

    public void SetAutoConvert(bool enabled)
        => avfilter_graph_set_auto_convert(_ptr, enabled ? 0 : 1u);

    public void RemoveFilter(FilterContext filterCtx) // down to filterctx?
    {   // review this (eg. remove twice the same filter)
        for (int i = 0; i < filters.Count; i++)
            if (filters[i]._ptr == filterCtx._ptr)
            {
                foreach(var pad in filters[i].InPads)
                    pad.FilterLink?.Unlink();

                foreach(var pad in filters[i].OutPads)
                    pad.FilterLink?.Unlink();

                filters.RemoveAt(i);

                break;
            }

        filterCtx.Free();
        filters.Remove(filterCtx);
    }

    public FFmpegResult RequestOldest()
        => new(avfilter_graph_request_oldest(_ptr));

    public string? Dump()
        => GetString(avfilter_graph_dump(_ptr, "")); // this will crash if we dont have names**

    public void RefreshFilters()
    {
        List<FilterContext> prevFilters = [.. filters];
        filters.Clear();
        filterCtxPtrToIndex.Clear();
        FilterContext? existing;

        for (int i = 0; i < _ptr->nb_filters; i++)
        {
            if ((existing = prevFilters.Where(f => f._ptr == _ptr->filters[i]).FirstOrDefault()) != null)
                filters.Add(existing); // required to have the same classes/instances with the user (for those that do not have ptr/dynamic only data)
            else
                filters.Add(FilterContext.Get(this, _ptr->filters[i]));

            filterCtxPtrToIndex[(nint)_ptr->filters[i]] = i;
        }

        RefreshLinks();
    }

    void RefreshLinks()
    {
        // Clear
        foreach(var filter in filters)
        {
            foreach(var pad in filter.InPads)
                pad.FilterLink = null;

            foreach(var pad in filter.OutPads)
                pad.FilterLink = null;
        }

        // Fill
        foreach(var filter in filters)
        {
            for(int i = 0; i < filter.InPads.Count; i++)
            {
                var inpad = filter.InPads[i];
                if (inpad.FilterLink != null)
                    continue; // already filled

                var avlink = filter._ptr->inputs[i];
                LinkPads(avlink, inpad);
            }

            for(int i = 0; i < filter.OutPads.Count; i++)
            {
                var outpad = filter.OutPads[i];
                if (outpad.FilterLink != null)
                    continue; // already filled

                var avlink = filter._ptr->outputs[i];
                LinkPads(avlink, outpad);
            }
        }
    }

    internal void LinkPads(AVFilterLink* avlink, FilterPadOut outpad, FilterPadIn? inpad = null)
    {
        if (avlink == null)
            return;

        var inpad2 = inpad ?? FindInPadFromPtrs(avlink->dst, avlink->dstpad)!;
        var link = FilterLink.Get(avlink, inpad2, outpad);
        outpad.FilterLink = inpad2.FilterLink = link;
    }

    internal void LinkPads(AVFilterLink* avlink, FilterPadIn inpad, FilterPadOut? outpad = null)
    {
        if (avlink == null)
            return;

        var outpad2 = outpad ?? FindOutPadFromPtrs(avlink->src, avlink->srcpad)!;
        var link = FilterLink.Get(avlink, inpad, outpad2);
        inpad.FilterLink = outpad2.FilterLink = link;
    }

    internal void AddFilterCtx(FilterContext filter)
    {
        filterCtxPtrToIndex[(nint)filter._ptr] = filters.Count;
        filters.Add(filter);
    }

    // TBR: required to reconstruct the graph after config
    public static int AVFILTERPAD_SIZE = SizeOf<AVFilterPad>();
    FilterPadIn? FindInPadFromPtrs(AVFilterContext* ctx, AVFilterPad* pad)
    {
        if (!filterCtxPtrToIndex.TryGetValue((nint)ctx, out int filterIndex))
            return null;

        var filter = filters[filterIndex];
        var padIndex  = (int)(((nint)pad - (nint)filter._ptr->input_pads) / AVFILTERPAD_SIZE);
        return padIndex < 0 || padIndex > filter.InPads.Count - 1 ? null : filter.InPads[padIndex];
    }

    FilterPadOut? FindOutPadFromPtrs(AVFilterContext* ctx, AVFilterPad* pad)
    {
        if (!filterCtxPtrToIndex.TryGetValue((nint)ctx, out int filterIndex))
            return null;

        var filter = filters[filterIndex];
        var padIndex  = (int)(((nint)pad - (nint)filter._ptr->output_pads) / AVFILTERPAD_SIZE);
        return padIndex < 0 || padIndex > filter.OutPads.Count - 1 ? null : filter.OutPads[padIndex];
    }

    #region Disposal
    ~FilterGraph()
    {
        if (!Disposed)
            Free();
    }

    public void Dispose()
    { 
        if (!Disposed)
        {
            Free();
            GC.SuppressFinalize(this);
        }
    }

    void Free()
    {
        fixed(AVFilterGraph** ptrPtr = &_ptr)
            avfilter_graph_free(ptrPtr);
    }
    #endregion
}
