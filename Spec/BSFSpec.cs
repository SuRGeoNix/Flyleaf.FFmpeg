namespace Flyleaf.FFmpeg.Spec;

public unsafe sealed partial class BSFSpec
{
    public FFmpegClassSpec?     AVClass     => FFmpegClassSpec.Get(_ptr->priv_class);
    public string               Name        { get; }
    public List<AVCodecID>      CodecIds    { get; }

    public readonly AVBitStreamFilter* _ptr;

    public static implicit operator AVBitStreamFilter*(BSFSpec bsfspec)
        => bsfspec._ptr;

    public BSFSpec(AVBitStreamFilter* bsf)
    {
        _ptr        = bsf;
        Name        = GetString(bsf->name)!;
        CodecIds    = GetCodecIds(bsf->codec_ids);
    }

    public static BSFSpec? FindBSFByName(string name) => BSFSpecsByName.TryGetValue(name, out var bsfspec) ? bsfspec : null;

    //public static readonly List<BSFSpec> BSFSpecs = [];
    public static readonly Dictionary<string, BSFSpec> BSFSpecsByName = [];

    internal static void FillBSFSpecs()
    {
        AVBitStreamFilter* cur;
        void* opaque = null;
        while ((cur = av_bsf_iterate(ref opaque)) != null)
        {
            var spec = new BSFSpec(cur);
            //BSFSpecs.Add(spec);
            BSFSpecsByName.Add(spec.Name, spec);
        }
    }
}
