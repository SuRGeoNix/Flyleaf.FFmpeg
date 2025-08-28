namespace Flyleaf.FFmpeg.Logging;

public class LogHandler
{
    public bool CanFatal;
    public bool CanError;
    public bool CanWarn;
    public bool CanInfo;
    public bool CanVerb;
    public bool CanDebug;
    public bool CanTrace;
    public bool CanMax;

    //public int?         Id                  { get; private set; }
    public string       Prefix              { get; private set; } = "";
    public string       FFmpegPrefix        { get; private set; } = "";
    public string       FFmpegPrevMsg       { get; internal set;} = "";
    public LogHandler?  Parent              { get; private set; }

    internal bool       included;
    
    public LogHandler(object moduleName, LogHandler? parent = null, string? strId = null, string? prefix = null, int? padding = null) :
        this(moduleName.GetType().Name, parent, strId, prefix, padding) { }

    //public LogHandler(string moduleName, LogHandler? parent = null, int? id = null, string? prefix = null, int? padding = null) :
    //    this(moduleName, parent, id != null ? $"#{id}" : null, prefix, padding) { Id = id; }

    public LogHandler(string moduleName, LogHandler? parent = null, string? strId = null, string? prefix = null, int? padding = null)
    {
        included = Log.IsModuleIncluded(moduleName);

        prefix ??= moduleName;
        //Id = id;
        //string strId = $"{id:D2}";
        Parent = parent;
        Prefix = 
            (parent != null ? parent.Prefix : "") + 
            (strId != null ? 
            $"[{(padding != null ? prefix.PadRight((int)padding) : prefix.PadRight(Log.config.Padding - strId.ToString()!.Length - 1))} {strId}] " : 
            $"[{(padding != null ? prefix.PadRight((int)padding) : prefix.PadRight(Log.config.Padding))}] ");

        if (included)
        {
            if (Log.config.LogLevel >= LogLevel.Fatal)
            {
                CanFatal = true;
                if (Log.config.LogLevel >= LogLevel.Error)
                {
                    CanError = true;
                    if (Log.config.LogLevel >= LogLevel.Warn)
                    {
                        CanWarn = true;

                        if (Log.config.LogLevel >= LogLevel.Info)
                        {
                            CanInfo = true;
                            if (Log.config.LogLevel >= LogLevel.Verb)
                            {
                                CanVerb = true;
                                if (Log.config.LogLevel >= LogLevel.Debug)
                                {
                                    CanDebug = true;
                                    if (Log.config.LogLevel >= LogLevel.Trace)
                                    {
                                        CanTrace = true;
                                        if (Log.config.LogLevel >= LogLevel.Max)
                                        {
                                            CanMax = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    //void UpdateCan() // TBR: should be updated if modules and/or loglevel changed?
    //{
    //    CanFatal	= included && Log.config.LogLevel >= LogLevel.Fatal;
    //    CanError	= included && Log.config.LogLevel >= LogLevel.Error;
    //    CanWarn     = included && Log.config.LogLevel >= LogLevel.Warn;
    //    CanInfo     = included && Log.config.LogLevel >= LogLevel.Info;
    //    CanVerb     = included && Log.config.LogLevel >= LogLevel.Verb;
    //    CanDebug    = included && Log.config.LogLevel >= LogLevel.Debug;
    //    CanTrace    = included && Log.config.LogLevel >= LogLevel.Trace;
    //    CanMax      = included && Log.config.LogLevel >= LogLevel.Max;
    //}

    public void SetFFmpegThreadData(string extraPrefix = "") // Should transfer as static to FFmpeg.Util.Log
    {
        FFmpegPrefix = "[" + $"FFmpeg{extraPrefix}".PadRight(Log.config.Padding) + "] ";
        Thread.SetData(Thread.GetNamedDataSlot($"log{Environment.CurrentManagedThreadId}"), this);
    }

    public static void UnSetFFmpegThreadData() // required?
        => Thread.FreeNamedDataSlot($"log{Environment.CurrentManagedThreadId}");

    public void Fatal(string msg)   { if (CanFatal) Log.WriteLineN($"{Prefix}{msg}", LogLevel.Fatal); }
    public void Error(string msg)   { if (CanError) Log.WriteLineN($"{Prefix}{msg}", LogLevel.Error); }
    public void Warn (string msg)   { if (CanWarn)  Log.WriteLineN($"{Prefix}{msg}", LogLevel.Warn);  }
    public void Info (string msg)   { if (CanInfo)  Log.WriteLineN($"{Prefix}{msg}", LogLevel.Info);  }
    public void Verb (string msg)   { if (CanVerb)  Log.WriteLineN($"{Prefix}{msg}", LogLevel.Verb);  }
    public void Debug(string msg)   { if (CanDebug) Log.WriteLineN($"{Prefix}{msg}", LogLevel.Debug); }
    public void Trace(string msg)   { if (CanTrace) Log.WriteLineN($"{Prefix}{msg}", LogLevel.Trace); }
    public void Max  (string msg)   { if (CanTrace) Log.WriteLineN($"{Prefix}{msg}", LogLevel.Max); }
    public void Level(string msg, LogLevel level)
                                    { if (included) Log.WriteLine($"{Prefix}{msg}", level); }

    // Use those if you manually check for Can<Level> to avoid string allocation
    public void FatalN(string msg)  => Log.WriteLineN($"{Prefix}{msg}", LogLevel.Fatal);
    public void ErrorN(string msg)  => Log.WriteLineN($"{Prefix}{msg}", LogLevel.Error);
    public void WarnN (string msg)  => Log.WriteLineN($"{Prefix}{msg}", LogLevel.Warn);
    public void InfoN (string msg)  => Log.WriteLineN($"{Prefix}{msg}", LogLevel.Info);
    public void VerbN (string msg)  => Log.WriteLineN($"{Prefix}{msg}", LogLevel.Verb);
    public void DebugN(string msg)  => Log.WriteLineN($"{Prefix}{msg}", LogLevel.Debug);
    public void TraceN(string msg)  => Log.WriteLineN($"{Prefix}{msg}", LogLevel.Trace);
    public void MaxN  (string msg)  => Log.WriteLineN($"{Prefix}{msg}", LogLevel.Max);
    public void LevelN(string msg, LogLevel level)
                                    => Log.WriteLineN($"{Prefix}{msg}", level);
}
