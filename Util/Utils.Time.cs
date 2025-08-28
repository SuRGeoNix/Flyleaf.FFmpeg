using System.Globalization;

namespace Flyleaf.FFmpeg;

public unsafe static partial class Utils
{
    public static int Compare(long ts1, AVRational tb1, long ts2, AVRational tb2)
        => av_compare_ts(ts1, tb1, ts2, tb2);

    public static long Rescale(long ts1, long tb1, long tb2, AVRounding rounding = AVRounding.NearInf)
        => av_rescale_rnd(ts1, tb1, tb2, rounding);

    public static long Rescale(long ts1, AVRational tb1, AVRational tb2, AVRounding rounding = AVRounding.NearInf)
        => av_rescale_q_rnd(ts1, tb1, tb2, rounding);

    public static string DoubleToTime(double d)     => d.ToString("0.000", CultureInfo.InvariantCulture);
    public static string DoubleToTimeMini(double d) => d.ToString("#.000", CultureInfo.InvariantCulture);

    public static string TicksToTime(long ticks)
    {
        if (ticks == NoTs)
            return "-";

        if (ticks == 0)
            return "00:00:00.000";

        return TsToTime(TimeSpan.FromTicks(ticks)); // TimeSpan.FromTicks(ticks).ToString("g");
    }
    public static string TicksToTimeMini(long ticks)
    {
        if (ticks == NoTs)
            return "-";

        if (ticks == 0)
            return "00.000";

        return TsToTimeMini(TimeSpan.FromTicks(ticks));
    }

    public static string McsToTime(long micro)
    {
        if (micro == NoTs)
            return "-";

        if (micro == 0)
            return "00:00:00.000";

        return TsToTime(TimeSpan.FromMicroseconds(micro));
    }

    public static string McsToTimeMini(long micro)
    {
        if (micro == NoTs)
            return "-";

        if (micro == 0)
            return "00.000";

        return TsToTimeMini(TimeSpan.FromMicroseconds(micro));
    }

    public static string TbToTime(long ts, AVRational tb)
    {
        if (ts == NoTs)
            return "-";

        if (ts == 0)
            return "00:00:00.000";

        return TsToTime(TimeSpan.FromTicks(Rescale(ts, tb, TimebaseTicks)));
    }

    public static string TbToTimeMini(long ts, AVRational tb)
    {
        if (ts == NoTs)
            return "-";

        if (ts == 0)
            return "00.000";

        return TsToTimeMini(TimeSpan.FromTicks(Rescale(ts, tb, TimebaseTicks)));
    }

    static string TsToTime(TimeSpan ts)
    {
        if (ts.Ticks > 0)
        {
            if (ts.TotalDays < 1)
                return ts.ToString(@"hh\:mm\:ss\.fff");
            else
                return ts.ToString(@"d\-hh\:mm\:ss\.fff");
        }

        if (ts.TotalDays > -1)
            return ts.ToString(@"\-hh\:mm\:ss\.fff");
        else
            return ts.ToString(@"\-d\-hh\:mm\:ss\.fff");
    }

    static string TsToTimeMini(TimeSpan ts)
    {
        if (ts.Ticks > 0)
        {
            if (ts.TotalMinutes < 1)
                return ts.ToString(@"ss\.fff");
            else if (ts.TotalHours < 1)
                return ts.ToString(@"mm\:ss\.fff");
            else if (ts.TotalDays < 1)
                return ts.ToString(@"hh\:mm\:ss\.fff");
            else
                return ts.ToString(@"d\-hh\:mm\:ss\.fff");
        }
        
        if (ts.TotalMinutes > -1)
            return ts.ToString(@"\-ss\.fff");
        else if (ts.TotalHours > -1)
            return ts.ToString(@"\-mm\:ss\.fff");
        else if (ts.TotalDays > -1)
            return ts.ToString(@"\-hh\:mm\:ss\.fff");
        else
            return ts.ToString(@"\-d\-hh\:mm\:ss\.fff");
    }
}
