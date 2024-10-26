namespace Flyleaf.FFmpeg;

public unsafe class FFmpegClass : FFmpegClassSpec
{
    /* TBR
     * 1) Performance for loops, consider single loop with a single condition
     * 2) Readonly does not necessary mean Export too (might have different property for exports?)
     * 3) We consider Const as 'child' of unit
     * 4) We consider unspecified (none) opt flags for ED AVS as for all
     */

    public AVClassCategory                  Category            => GetCategory();
    public FFmpegClass?                     Child               => GetChild();

    public IEnumerable<FFmpegOption>        OptionsConstAll     => base.OptionsConst;
    public new IEnumerable<FFmpegOption>    OptionsConst        => optFlags == OptFlags.None ? OptionsConstAll : OptionsConstAll.Where(x => 
            ((x.Flags & DE)     == OptFlags.None || (x.Flags & (de)) == de) 
        &&  ((x.Flags & AVS)    == OptFlags.None || (x.Flags & avs ) == avs));
    public IEnumerable<FFmpegOption>        OptionsConstRW      => OptionsConst.Where(x => !x.Flags.HasFlag(OptFlags.Readonly));
    public IEnumerable<FFmpegOption>        OptionsConstRO      => OptionsConst.Where(x =>  x.Flags.HasFlag(OptFlags.Readonly));

    public IEnumerable<FFmpegOption>        OptionsAll          => base.Options;
    public new IEnumerable<FFmpegOption>    Options             => optFlags == OptFlags.None ? OptionsAll : OptionsAll.Where(x => 
            ((x.Flags & DE)     == OptFlags.None || (x.Flags & (de)) == de) 
        &&  ((x.Flags & AVS)    == OptFlags.None || (x.Flags & avs ) == avs));
    public IEnumerable<FFmpegOption>        OptionsRW           => Options.Where(x => !x.Flags.HasFlag(OptFlags.Readonly));
    public IEnumerable<FFmpegOption>        OptionsRO           => Options.Where(x =>  x.Flags.HasFlag(OptFlags.Readonly));

    public Dictionary<string, string>       ValuesAll           => OptionsAll.  ToDictionary(k => k.Name, v => GetData(v.Name).result!);
    public Dictionary<string, string>       Values              => Options.     ToDictionary(k => k.Name, v => GetData(v.Name).result!);
    public Dictionary<string, string>       ValuesRW            => OptionsRW.   ToDictionary(k => k.Name, v => GetData(v.Name).result!);
    public Dictionary<string, string>       ValuesRO            => OptionsRO.   ToDictionary(k => k.Name, v => GetData(v.Name).result!);
    
    OptFlags optFlags = OptFlags.None;
    OptFlags de;
    OptFlags avs;

    FFmpegClass? GetChild()
    {
        if (@class->child_next.Pointer == 0)
            return null;

        var ChildNext = GetDelegateForFunctionPointer<AVClass_child_next>(@class->child_next.Pointer); // same as av_opt_child_next
        void* childCtx;
        
        return 
            ((childCtx = ChildNext(ctx, null)) != null) ?
            new(childCtx) : 
            null;
    }

    AVClassCategory GetCategory()
    {
        if (@class->get_category.Pointer == 0)
            return AVClassCategory.Na;

        var GetCategoryX = GetDelegateForFunctionPointer<AVClass_get_category>(@class->get_category.Pointer);
        return GetCategoryX(ctx);
    }

    private readonly void* ctx;
    private FFmpegClass(void* ctx) : base(*(AVClass**)ctx)
        => this.ctx = ctx;

    private FFmpegClass(void* ctx, OptFlags optFlags) : base(*(AVClass**)ctx)
    {
        this.ctx        = ctx;
        this.optFlags   = optFlags;
        de  = optFlags & (OptFlags.DecodingParam | OptFlags.EncodingParam);
        avs = optFlags & (OptFlags.AudioParam | OptFlags.VideoParam | OptFlags.SubtitleParam);
    }
    
    public static FFmpegClass? Get(void* ctx)
        => ctx == null || *(AVClass**)ctx == null ? null : new(ctx);

    public static FFmpegClass? Get(void* ctx, OptFlags optFlags)
        => ctx == null || *(AVClass**)ctx == null ? null : new(ctx, optFlags);

    public FFmpegOption? Find(string name, string unit, OptFlags optFlags = default, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        AVOption* val = av_opt_find(ctx, name, unit, optFlags, searchFlags);
        return val == null ? null : new(val);
    }

    public (FFmpegOption? option, FFmpegClass? optCtx) Find2(string name, string unit, OptFlags optFlags = default, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        void* obj;
        AVOption* val = av_opt_find2(ctx, name, unit, optFlags, searchFlags, &obj);
        return val == null ? (null, null) : (new(val), obj == ctx ? this : new(obj));
    }

    public void SetDefaults(OptFlags mask = default, OptFlags flags = default)
        => av_opt_set_defaults2(ctx, mask, flags);

    public FFmpegResult Set(string name, string? value, OptSearchFlags searchFlags = OptSearchFlags.Children)
        => new(av_opt_set(ctx, name, value, searchFlags));

    public FFmpegResult Set(string name, long value, OptSearchFlags searchFlags = OptSearchFlags.Children)
        => new(av_opt_set_int(ctx, name, value, searchFlags));

    public FFmpegResult Set(string name, bool value, OptSearchFlags searchFlags = OptSearchFlags.Children)
        => new(av_opt_set_int(ctx, name, value ? 1L : 0L, searchFlags));

    public FFmpegResult Set<T>(string name, T value, OptSearchFlags searchFlags = OptSearchFlags.Children) where T : Enum
        => Set(name, Convert.ToInt64(value), searchFlags);

    public FFmpegResult Set(string name, double value, OptSearchFlags searchFlags = OptSearchFlags.Children)
        => new(av_opt_set_double(ctx, name, value, searchFlags));

    public FFmpegResult Set(string name, AVRational value, OptSearchFlags searchFlags = OptSearchFlags.Children)
        => new(av_opt_set_q(ctx, name, value, searchFlags));

    public FFmpegResult Set(string name, byte* data, int dataCount, OptSearchFlags searchFlags = OptSearchFlags.Children)
        => new(av_opt_set_bin(ctx, name, data, dataCount, searchFlags));

    public FFmpegResult Set<T>(string name, T[] value, OptSearchFlags searchFlags = OptSearchFlags.Children) where T : unmanaged
    {
        //Span<int> arr = stackalloc int[value.Length];
        //for (int i = 0; i < arr.Length; i++)
        //    arr[i] = Convert.ToInt32(value[i]);

        fixed(T* ptr = value)
            return new(av_opt_set_bin(ctx, name, (byte*)ptr, sizeof(int) * value.Length, searchFlags));
    }

    //public int Set<T>(string name, List<T> value, OptSearchFlags searchFlags = OptSearchFlags.Children) where T : Enum
    //{
    //    Span<int> arr = stackalloc int[value.Count];
    //    for (int i = 0; i < arr.Length; i++)
    //        arr[i] = Convert.ToInt32(value[i]);
        
    //    fixed(int* ptr = arr)
    //        return av_opt_set_bin(ctx, name, (byte*)ptr, sizeof(int) * arr.Length, searchFlags);
    //}

    //public int Set(string name, List<int> value, OptSearchFlags searchFlags = OptSearchFlags.Children)
    //{
    //    Span<int> arr = stackalloc int[value.Count];
    //    for (int i = 0; i < arr.Length; i++)
    //        arr[i] = value[i];

    //    fixed(int* ptr = arr)
    //        return av_opt_set_bin(ctx, name, (byte*)ptr, sizeof(int) * arr.Length, searchFlags);
    //}

    public FFmpegResult Set(string name, int[] value, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        fixed(int* ptr = value)
            return new(av_opt_set_bin(ctx, name, (byte*)ptr, sizeof(int) * value.Length, searchFlags));
    }

    public FFmpegResult Set(string name, ulong[] value, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        fixed(ulong* ptr = value)
            return new(av_opt_set_bin(ctx, name, (byte*)ptr, sizeof(ulong) * value.Length, searchFlags));
    }

    public FFmpegResult Set(string name, AVChannelLayout value, OptSearchFlags searchFlags = OptSearchFlags.Children)
        => new(av_opt_set_chlayout(ctx, name, &value, searchFlags));

    public FFmpegResult Set(string name, int width, int height, OptSearchFlags searchFlags = OptSearchFlags.Children)
        => new(av_opt_set_image_size(ctx, name, width, height, searchFlags));

    public FFmpegResult Set(string name, AVPixelFormat value, OptSearchFlags searchFlags = OptSearchFlags.Children) 
        => new(av_opt_set_pixel_fmt(ctx, name, value, searchFlags));

    public FFmpegResult Set(string name, AVSampleFormat value, OptSearchFlags searchFlags = OptSearchFlags.Children)
        => new(av_opt_set_sample_fmt(ctx, name, value, searchFlags));

    public FFmpegResult SetVideoRate(string name, AVRational value, OptSearchFlags searchFlags = OptSearchFlags.Children)
        => new(av_opt_set_video_rate(ctx, name, value, searchFlags));

    public FFmpegResult Set(string name, Dictionary<string, string> value, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        AVDictionary* avdict = AVDictFromDict(value);
        FFmpegResult ret = new(av_opt_set_dict_val(ctx, name, avdict, searchFlags));
        av_dict_free(ref avdict);
        return ret;
    }

    public (FFmpegResult success, string?  result) GetData(string name, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        byte* outVal;
        FFmpegResult ret = new(av_opt_get(ctx, name, searchFlags, &outVal));
        return (ret, GetString(outVal));
    }

    public (FFmpegResult success, long result) GetLong(string name, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        long val;
        FFmpegResult ret = new(av_opt_get_int(ctx, name, searchFlags, & val));
        return (ret, val);
    }

    public (FFmpegResult success, bool result) GetBool(string name, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        (FFmpegResult ret, long result) = GetLong(name, searchFlags);
        return (ret, result != 0);
    }

    public (FFmpegResult success, double result) GetDouble(string name, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        double val;
        FFmpegResult ret = new(av_opt_get_double(ctx, name, searchFlags, & val));
        return (ret, val);
    }

    public (FFmpegResult success, AVRational result) GetRational(string name, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        AVRational rational;
        FFmpegResult ret = new(av_opt_get_q(ctx, name, searchFlags, & rational));
        return (ret, rational);
    }

    public (FFmpegResult success, int width, int height) GetImageSize(string name, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        int width, height;
        FFmpegResult ret = new(av_opt_get_image_size(ctx, name, searchFlags, & width, & height));
        return (ret, width, height);
    }

    public (FFmpegResult success, AVPixelFormat result) GetPixelFormat(string name, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        AVPixelFormat pixelFormat;
        FFmpegResult ret = new(av_opt_get_pixel_fmt(ctx, name, searchFlags, & pixelFormat));
        return (ret, pixelFormat);
    }

    public (FFmpegResult success, AVRational result) GetVideoRate(string name, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        AVRational rational;
        FFmpegResult ret = new(av_opt_get_video_rate(ctx, name, searchFlags, & rational));
        return (ret, rational);
    }

    public (FFmpegResult success, AVChannelLayout result) GetChannelLayout(string name, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        AVChannelLayout chLayout;
        FFmpegResult ret = new(av_opt_get_chlayout(ctx, name, searchFlags, & chLayout)); // TBR: must be freed/uninit by the caller
        return (ret, chLayout);
    }

    public (FFmpegResult success, AVSampleFormat result) GetSampleFormat(string name, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        AVSampleFormat sampleFormat;
        FFmpegResult ret = new(av_opt_get_sample_fmt(ctx, name, searchFlags, & sampleFormat));
        return (ret, sampleFormat);
    }

    public (FFmpegResult success, Dictionary<string, string>? result) GetDictionary(string name, OptSearchFlags searchFlags = OptSearchFlags.Children)
    {
        AVDictionary* dict;
        FFmpegResult ret = new(av_opt_get_dict_val(ctx, name, searchFlags, & dict));
        return (ret, AVDictToDict(dict));
    }
}
