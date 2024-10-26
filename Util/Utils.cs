using System.Buffers.Binary;

namespace Flyleaf.FFmpeg;

public unsafe static partial class Utils
{
    /* TODO
     * 1) Support dynamic loading (not preload) + mix? (pre-load main except dynamic for filters/devices) | Problem wiht DllImportResolver dependencies - it will not ask us to manually find them
     * 2) Support Task and ensure comleted during DllImportResolver?
     * 3) Review OS related maybe preprocessor directives and version validation (validate also x86/x64?)
     * 
     * NOTES
     * 1) Pre-loading make sense when we run the process as a service (eg. opening new tasks/windows but we keep the hashed tables / libraries already loaded)
     *      trade-off initial delay vs fast loading while running
     */

    public static void LoadLibraries(string path, LoadProfile profile = LoadProfile.All, bool validateVersion = true, bool fillTables = true)
    {
        Raw.LoadLibraries(path, profile, validateVersion);
        
        if (fillTables)
        {
            FillHWDevices();
            FillCodecDescriptors();
            FillCodecSpecs();
            FillCodecParserSpecs();
            FillFormatSpecs();
            FillBSFSpecs();

            if (profile != LoadProfile.Main)
                FillFilterSpecs();

            // PixFmtDescriptors, Parsers ?
        }
    }

    public static string[]? GetStringsFromComma(byte* csStr)
        => csStr != null ? GetString(csStr)!.Split(',') : null;

    public static string? GetString(byte* ptr)
        => PtrToStringUTF8((nint) ptr);
    
    public static string? GetString(nint ptr)
        => PtrToStringUTF8(ptr);

    public static string GetName(AVChannelLayout* chLayout)
    {
        var chBytes = new byte[32];
        fixed(byte* chBytesPtr = chBytes)
        {
            int err = av_channel_layout_describe(chLayout, chBytesPtr, 32);
            if (err < 1) return "";

            return GetString(chBytesPtr)!;
        }
    }

    public static int GetBitsPerPixel(AVPixFmtDescriptor* pixDesc)
        => av_get_bits_per_pixel(pixDesc);

    public static int GetPaddedBitsPerPixel(AVPixFmtDescriptor* pixDesc)
        => av_get_padded_bits_per_pixel(pixDesc);

    public static AVPixelFormat GetPixelFormat(string name)
        => av_get_pix_fmt(name);

    public static AVSampleFormat GetSampleFormat(string name)
        => av_get_sample_fmt(name);

    public static int Compare(long ts1, AVRational tb1, long ts2, AVRational tb2)
        => av_compare_ts(ts1, tb1, ts2, tb2);

    public static long Rescale(long ts1, long tb1, long tb2, AVRounding rounding = AVRounding.NearInf)
        => av_rescale_rnd(ts1, tb1, tb2, rounding);

    public static long Rescale(long ts1, AVRational tb1, AVRational tb2, AVRounding rounding = AVRounding.NearInf)
        => av_rescale_q_rnd(ts1, tb1, tb2, rounding);

    public static void AVStrDupReplace(string? str, byte** strPtr)
    {
        if (strPtr == null)
            return;

        av_free(strPtr);

        if (str != null)
            *strPtr = av_strdup(str);
    }

    public static string GetFourCCString(int fourcc)
        => GetFourCCString((uint)fourcc);

    public static string GetFourCCString(uint fourcc)
    {
        byte* t1 = (byte*)av_mallocz(AV_FOURCC_MAX_STRING_SIZE);
        av_fourcc_make_string(t1, fourcc);
        string ret = GetString(t1)!;
        av_free(t1);
        return ret;
    }

    public static string ToFourCC(uint version)
        => ToFourCC((int)version);

    public static string ToFourCC(int version)
        => string.Join(".", BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(version)).SkipWhile(v => v == 0));

    internal static void AddToDicList<TKey, TValue>(Dictionary<TKey, List<TValue>> dic, TKey key, TValue value) where TKey : notnull
    {
        if (dic.TryGetValue(key, out var spec))
            spec.Add(value);
        else
            dic.Add(key, [value]);
    }
}
