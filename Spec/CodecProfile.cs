namespace Flyleaf.FFmpeg.Spec;

public unsafe sealed partial class CodecProfile
{
    public string   Name        { get; }
    public int      Profile     { get; }

    CodecProfile(string name, int profile) { Name = name; Profile = profile; }

    CodecProfile(AVProfile* profile)
    {
        Name    = GetString(profile->name)!;
        Profile = profile->profile;
        ProfilesByPtr.Add((nint)profile, this); 
    }
}