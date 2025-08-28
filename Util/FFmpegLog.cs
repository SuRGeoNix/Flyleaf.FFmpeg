namespace Flyleaf.FFmpeg;

public static class FFmpegLog
{
    static av_log_set_callback_callback? LogClbkDlg;

    public static void SetLogLevel(LogLevel logLevel)
        => av_log_set_level(logLevel);

    public static LogLevel GetLogLevel()
        => av_log_get_level();

    public static void UnSetCallback()
    {
        LogClbkDlg = null;
        av_log_set_callback(null);
    }

    public unsafe static void SetDefaultCallback()
    {
        LogClbkDlg = av_log_default_callback;
        av_log_set_callback(LogClbkDlg);
    }

    /// <summary>
    /// Sets FFmpeg log to custom callback
    /// </summary>
    /// <param name="logCallback">The callback must be thread safe, even if the application does not use threads itself as some codecs are multithreaded.</param>
    public static void SetCallback(av_log_set_callback_callback logCallback)
    {
        LogClbkDlg = logCallback;
        av_log_set_callback(LogClbkDlg);
    }
}
