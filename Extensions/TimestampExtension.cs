namespace Flyleaf.FFmpeg.Extensions;

public static class TimestampExtension
{
    public static long ToMs(this long timestamp, AVRational timebase)
        => Rescale(timestamp, timebase, TimebaseMs);

    public static long ToMcs(this long timestamp, AVRational timebase)
        => Rescale(timestamp, timebase, TimebaseMcs);

    public static string McsToTime(this long micro, AVRational timebase)
    {
        if (micro == NoTs)
            return "-";

        return Rescale(micro, timebase, TimebaseMcs).McsToTime();
    }

    public static string McsToTime(this long micro)
    {
        if (micro == NoTs)
            return "-";

        var ts = TimeSpan.FromMicroseconds(micro);
        string sign = micro < 0 ? "-" : "";

        if (ts.Days > 0)
            return sign + ts.ToString(@"d\-hh\:mm\:ss\.fff");
        else if (ts.Hours > 0)
            return sign + ts.ToString(@"hh\:mm\:ss\.fff");
        else if (ts.Minutes > 0)
            return sign + ts.ToString(@"mm\:ss\.fff");
        else
            return sign + ts.ToString(@"ss\.fff");
    }
}
