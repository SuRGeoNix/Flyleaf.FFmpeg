namespace Flyleaf.FFmpeg.Filter;

public unsafe abstract class FilterPad : FilterPadSpec
{
    public FilterLink?      FilterLink      { get; internal set; }
    public FilterContext    FilterContext   { get; internal set; }

    internal FilterPad(FilterContext filterContext, AVFilterPad* pad, int index) : base(pad, index)
        => FilterContext = filterContext;
}

public unsafe class FilterPadOut : FilterPad
{
    public FilterPadIn? LinkedTo => FilterLink?.InPad;

    internal FilterPadOut(FilterContext filterContext, AVFilterPad* pad, int index) : base(filterContext, pad, index) { }

    public int Link(FilterContext filterContext)
        => Link(filterContext.InPads[0]);

    public int Link(FilterPadIn pad)
    {
        int ret = avfilter_link(FilterContext._ptr, (uint)Index, pad.FilterContext, (uint)pad.Index);

        if (ret < 0)
            return ret;

        var avlink = FilterContext._ptr->outputs[Index];
        FilterContext.FilterGraph.LinkPads(avlink, this, pad);

        return ret;
    }
}

public unsafe class FilterPadIn : FilterPad
{
    public FilterPadOut? LinkedFrom => FilterLink?.OutPad;

    internal FilterPadIn(FilterContext filterContext, AVFilterPad* pad, int index) : base(filterContext, pad, index) { }
}
