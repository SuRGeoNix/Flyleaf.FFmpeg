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

    public static bool FFmpegLoaded { get; private set; }

    internal static object ffmpegLocker = new();

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

            // Protocols
            // PixFmtDescriptors, Parsers ?
        }

        lock (ffmpegLocker)
        {
            FFmpegLoaded = true;
            if (Log.LogLoaded)
                Log.SetAVLog();
            else
                Log.Start();
        }

        // TBR: Print versions / diffs between bindings and ffmpeg libs?
        //log = new("FFmpeg Loader");
        //log.Debug($"avformat {ToFourCC(avformat_version())} | {LIBAVFORMAT_VERSION_MAJOR}.{LIBAVFORMAT_VERSION_MINOR}.{LIBAVFORMAT_VERSION_MICRO}");
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

    public static List<T> GetFlagsAsList<T>(T value) where T : Enum
    {
        List<T> values = [];

        var enumValues = Enum.GetValuesAsUnderlyingType(typeof(T));
        //var enumValues = Enum.GetValues(typeof(T)); // breaks AOT?

        foreach(T flag in enumValues)
            if (value.HasFlag(flag) && flag.ToString() != "None")
                values.Add(flag);

        return values;
    }
    public static string? GetFlagsAsString<T>(T value, string separator = " | ") where T : Enum
    {
        string? ret = null;
        List<T> values = GetFlagsAsList(value);

        if (values.Count == 0)
            return ret;

        for (int i = 0; i < values.Count - 1; i++)
            ret += values[i] + separator; 

        return ret + values[^1];
    }

    public static int GetBitsPerPixel(AVPixFmtDescriptor* pixDesc)
        => av_get_bits_per_pixel(pixDesc);

    public static int GetPaddedBitsPerPixel(AVPixFmtDescriptor* pixDesc)
        => av_get_padded_bits_per_pixel(pixDesc);

    public static AVPixelFormat GetPixelFormat(string name)
        => av_get_pix_fmt(name);

    public static AVSampleFormat GetSampleFormat(string name)
        => av_get_sample_fmt(name);

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
}
