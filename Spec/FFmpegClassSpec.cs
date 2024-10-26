namespace Flyleaf.FFmpeg.Spec;

public unsafe class FFmpegClassSpec
{
    public string                       Name                => GetString(@class->class_name)!;
    public string                       Version             => ToFourCC(@class->version);
    public AVClassCategory              CategorySpec        => @class->category;
    public IEnumerable<FFmpegOption>    OptionsConst        => GetOptions().Where(x => x.Type == AVOptionType.Const);
    public IEnumerable<FFmpegOption>    Options             => GetOptions().Where(x => x.Type != AVOptionType.Const);
    public List<FFmpegClassSpec>        ChildsPossible      => GetPossibleChilds();

    List<FFmpegClassSpec> GetPossibleChilds()
    {
        if (@class->child_class_iterate.Pointer == 0)
            return [];

        List<FFmpegClassSpec> childs = [];
        var ChildClassIterate = GetDelegateForFunctionPointer<AVClass_child_class_iterate>(@class->child_class_iterate.Pointer); // same as av_opt_child_class_iterate
        void* iter = null;
        AVClass* curChild;
        while ((curChild = ChildClassIterate(&iter)) != null)
            childs.Add(new(curChild));

        return childs;
    }

    public IEnumerable<FFmpegOption> GetOptions()
        => ReadSequence(
        p:              (IntPtr)@class->option,
        unitSize:       sizeof(AVOption),
        exitCondition:  _p => ((AVOption*)_p)->name == null,
        valGetter:      _p => new FFmpegOption((AVOption*)_p));

    private static IEnumerable<T> ReadSequence<T>(IntPtr p, int unitSize, Func<IntPtr, bool> exitCondition, Func<IntPtr, T> valGetter)
    {
        if (p == IntPtr.Zero)
            yield break;
        
        while (!exitCondition(p))
        {
            yield return valGetter(p);
            p += unitSize;
        }
    }

    internal readonly AVClass* @class;

    internal FFmpegClassSpec(AVClass* @class)
        => this.@class = @class;

    public static FFmpegClassSpec? Get(AVClass* @class)
        => @class == null ? null : new(@class);
}
