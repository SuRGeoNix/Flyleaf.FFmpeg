namespace Flyleaf.FFmpeg.Spec;

public unsafe sealed partial class FilterSpec
{
    // TODO: hw with wrappers? FilterSpecs.Where(x => x.Flags.HasFlag(FilterFlags.Hwdevice))
    // byPtr?

    public static FilterSpec? FindFilter(string name) => FilterSpecsByName.TryGetValue(name, out var spec) ? spec : null;
    

    public static readonly List<FilterSpec> FilterSpecs = [];
    public static readonly Dictionary<string, FilterSpec> FilterSpecsByName = [];

    internal static void FillFilterSpecs()
    {
        AVFilter* cur;
        void* opaque = null;
        while ((cur = av_filter_iterate(ref opaque)) != null)
        {
            var spec = new FilterSpec(cur);
            FilterSpecs.Add(spec);
            FilterSpecsByName.Add(spec.Name, spec);
        }
    }

    //public static FilterSpec? FindByName(string name)
    //{
    //    var avfilter = avfilter_get_by_name(name);
    //    if (avfilter == null)
    //        return null;

    //    return new(avfilter);
    //}
}
