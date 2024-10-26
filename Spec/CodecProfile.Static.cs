namespace Flyleaf.FFmpeg.Spec;

public unsafe sealed partial class CodecProfile
{
    public static string GetProfileName(List<CodecProfile> profiles, int profile)
    {
        if (profile != AV_PROFILE_UNKNOWN)
            foreach(var cprofile in profiles)
                if (cprofile.Profile == profile)
                    return cprofile.Name;

        return PROFILE_UNKNOWN.Name;
    }

    public static CodecProfile GetProfile(List<CodecProfile> profiles, int profile)
    {
        if (profile != AV_PROFILE_UNKNOWN)
            foreach(var cprofile in profiles)
                if (cprofile.Profile == profile)
                    return cprofile;

        return PROFILE_UNKNOWN;
    }

    public static CodecProfile GetProfile(AVProfile* input)
        => ProfilesByPtr.TryGetValue((nint)input, out var cache) ? cache : new(input);

    public static Dictionary<nint, CodecProfile> ProfilesByPtr = [];
    public static readonly CodecProfile PROFILE_UNKNOWN = new("Unknown", AV_PROFILE_UNKNOWN);
}
