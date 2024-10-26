namespace Flyleaf.FFmpeg.Spec;

public unsafe sealed partial class FilterSpec
{
    public FFmpegClassSpec?                     AVClass         => FFmpegClassSpec.Get(_ptr->priv_class);
    public string                               Name            => GetString(_ptr->name)!;
    public string?                              Description     => GetString(_ptr->description);
    public FilterFlags                          Flags           => _ptr->flags;

    // TODO single property?*
    public FilterFormatsState                   PadsType        => (FilterFormatsState)_ptr->formats_state;
    public AVSampleFormat                       SampleFormat    => PadsType == FilterFormatsState.Audio ? _ptr->formats.sample_fmt : AVSampleFormat.None;
    public List<AVSampleFormat>                 SampleFormats   => PadsType == FilterFormatsState.AudioMulti ? GetSampleFormats(_ptr->formats.samples_list) : [];
    public AVPixelFormat                        PixelFormat     => PadsType == FilterFormatsState.Video ? _ptr->formats.pix_fmt : AVPixelFormat.None;
    public List<AVPixelFormat>                  PixelFormats    => PadsType == FilterFormatsState.VideoMulti ? GetPixelFormats(_ptr->formats.pixels_list) : [];

    public ReadOnlyCollection<FilterPadInSpec>  InPads          { get; private set; } = null!;
    public ReadOnlyCollection<FilterPadOutSpec> OutPads         { get; private set; } = null!;
    List<FilterPadInSpec>   inpads = [];
    List<FilterPadOutSpec>  outpads = [];

    public readonly AVFilter* _ptr;

    public static implicit operator AVFilter*(FilterSpec filter)
        => filter._ptr;

    internal FilterSpec(AVFilter* filter)
    {
        _ptr    = filter;
        InPads  = new(inpads);
        OutPads = new(outpads);

        for(int i = 0; i < filter->nb_inputs; i++)
            inpads.Add(new(filter->inputs, i));

        for(int i = 0; i < filter->nb_outputs; i++)
            outpads.Add(new(filter->outputs, i));

    }

    public string TestDump()
    {
        // inputs / output / name / padstype / flags?
        string dump = "[in ";

        foreach(var input in inpads)
            dump += $"{input.Name} {(input.Type == AVMediaType.Audio ? "A" : (input.Type == AVMediaType.Video ? "V" : "O"))}, ";
        dump += "]";

        dump += $"\t\t {Name} ({PadsType}) [out ";

        foreach(var input in outpads)
            dump += $"{input.Name} {(input.Type == AVMediaType.Audio ? "A" : (input.Type == AVMediaType.Video ? "V" : "O"))}, ";
        dump += "]";

        return dump;
    }

    public unsafe class FilterPadSpec
    {
        public string?          Name        => avfilter_pad_get_name(_ptr, index);
        public AVMediaType      Type        => avfilter_pad_get_type(_ptr, index);
        public int              Index       => index;

        public readonly AVFilterPad* _ptr;
        public static implicit operator AVFilterPad*(FilterPadSpec pad)
            => pad._ptr;

        int index;

        internal FilterPadSpec(AVFilterPad* pad, int index)
        {
            _ptr = pad;
            this.index = index;
        }
    }

    public unsafe class FilterPadOutSpec : FilterPadSpec
    {
        internal FilterPadOutSpec(AVFilterPad* pad, int index) : base(pad, index) { }
    }

    public unsafe class FilterPadInSpec : FilterPadSpec
    {
        internal FilterPadInSpec(AVFilterPad* pad, int index) : base(pad, index) { }
    }
}

// Warning this is internal!
public enum FilterFormatsState
{
    Passthrough,
    QueryFunc,
    VideoMulti,
    AudioMulti,
    Video,
    Audio
}