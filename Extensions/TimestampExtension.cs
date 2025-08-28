namespace Flyleaf.FFmpeg.Extensions;

public static class TimestampExtension
{
    public static long ToMs(this long timestamp, AVRational timebase)
        => Rescale(timestamp, timebase, TimebaseMs);

    public static long ToMcs(this long timestamp, AVRational timebase)
        => Rescale(timestamp, timebase, TimebaseMcs);

    public static long FromMcs(this long timestamp, AVRational timebase)
        => Rescale(timestamp, TimebaseMcs, timebase);

    public static long FromMcs(this double timestamp, AVRational timebase)
        => FromMcs((long)timestamp, timebase);

    public static string DoubleToTime(this double d)
        => Utils.DoubleToTime(d);

    public static string DoubleToTimeMini(this double d)
        => Utils.DoubleToTimeMini(d);

    public static string TicksToTime(this long ticks)
        => Utils.TicksToTime(ticks);

    public static string TicksToTimeMini(this long ticks)
        => Utils.TicksToTimeMini(ticks);

    public static string McsToTime(this long micro)
        => Utils.McsToTime(micro);

    public static string McsToTimeMini(this long micro)
        => Utils.McsToTimeMini(micro);

    public static string TbToTime(this long ts, AVRational timebase)
        => Utils.TbToTime(ts, timebase);

    public static string TbToTimeMini(this long ts, AVRational timebase)
        => Utils.TbToTimeMini(ts, timebase);
}
