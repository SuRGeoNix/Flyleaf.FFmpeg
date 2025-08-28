namespace Flyleaf.FFmpeg.Logging;

public class LogConfig
{
    #if DEBUG
    public LogLevel LogLevel        { get; set; } = LogLevel.Debug;
    #else
    public LogLevel LogLevel        { get; set; } = LogLevel.Quiet;
    #endif
    public LogLevel FFmpegLogLevel  { get => ffmpegLogLevel; set { ffmpegLogLevel = value; if (FFmpegLoaded) Log.SetAVLog(); } }
    public int      Padding         { get; set; } = 12;
    public bool     Append          { get; set; }
    public string   Output          { get => output; set { output = value; if (Log.LogLoaded) Log.SetOutput(); /*if (Engine.FFmpegLoaded) Log.SetAVLog();*/ } }
    public string   DateTimeFormat  { get; set; } = "HH:mm:ss.fff";
    public int      CachedLines     { get; set; } = 20;
    public string   EnabledModules  { get; set; } = "";
    public string   DisabledModules { get; set; } = "";

    #if DEBUG
    string output = ":console";
    LogLevel ffmpegLogLevel = LogLevel.Debug;
    #else
    string output = "";
    LogLevel ffmpegLogLevel = LogLevel.Quiet;
    #endif
    
}
