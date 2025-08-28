using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Flyleaf.FFmpeg.Logging;

public unsafe static class Log
{
    /* TODO
     * 
     * Instead of enable/disable modules have <module, level> to specify log level for a specific module and have also the default log level
     * Config: allow logging without date
     */

    public static bool LogLoaded { get; private set; }

    internal static LogConfig config = null!;
   
    private static Action<string>
                        Output          = DevNullPtr;
    static string       lastOutput      = "";

    static ConcurrentQueue<byte[]>
                        fileData        = [];
    static bool         fileTaskRunning;
    static FileStream?  fileStream;
    static object       lockFileStream  = new();
    static Dictionary<LogLevel, string>
                        logLevels       = [];

    readonly static HashSet<string> 
                        enabledModules  = [];
    readonly static HashSet<string> 
                        disabledModules = [];

    public static bool IsModuleIncluded(string moduleName)
    {
        var modLower = moduleName.ToLower();

        bool excluded = 
            (enabledModules.Count > 0 && !enabledModules.Contains(modLower)) ||
            (disabledModules.Count > 0 && disabledModules.Contains(modLower));

        return !excluded;
    }

    public static void Start(LogConfig? cfg = null)
    {
        if (LogLoaded)
            return;

        config = cfg ?? new();

        int maxLogLevelLength = 5;
        foreach (LogLevel loglevel in Enum.GetValuesAsUnderlyingType(typeof(LogLevel)))
            if (loglevel.ToString().Length > maxLogLevelLength)
                maxLogLevelLength = loglevel.ToString().Length;

        foreach (LogLevel loglevel in Enum.GetValuesAsUnderlyingType(typeof(LogLevel)))
            logLevels.Add(loglevel, " | " + loglevel.ToString().PadRight(maxLogLevelLength) + " | ");

        if (config.EnabledModules != "")
        {
            var mods = config.EnabledModules.Split(',');
            foreach (var module in mods)
                enabledModules.Add(module.Trim().ToLower());
        }

        if (config.DisabledModules != "")
        {
            var mods = config.DisabledModules.Split(',');
            foreach (var module in mods)
                disabledModules.Add(module.Trim().ToLower());
        }

        SetOutput();

        // Flush File Data on Proces Exit
        Process.GetCurrentProcess().Exited += (o, e) =>
        {
            lock (lockFileStream)
            {
                if (fileStream == null)
                    return;

                while (fileData.TryDequeue(out byte[]? data))
                    fileStream.Write(data, 0, data.Length);

                fileStream.Dispose();   
            }
        };

        fixed (int* ptr = &printPrefix)
            printPrefixPtr = ptr;

        bufferLogLine   = AllocHGlobal(1024);
        logGlobal       = new("FFmpeg");
        //LogClbkDlg      = LogClbk;
        LogLoaded       = true;

        lock (ffmpegLocker)
        {
            if (FFmpegLoaded)
                SetAVLog();
        }
    }

    internal static void SetOutput()
    {
        string output = config.Output;

        if (string.IsNullOrEmpty(output))
        {
            if (lastOutput != "")
            {
                Output = DevNullPtr;
                lastOutput = "";
            }
        }
        else if (output.StartsWith(':'))
        {
            if (output == ":console")
            {
                if (lastOutput != ":console")
                {
                    Output = Console.Write;
                    lastOutput = ":console";
                }
            }
            else if (output == ":debug")
            {
                if (lastOutput != ":debug")
                {
                    Output = DebugPtr;
                    lastOutput = ":debug";
                }
            }
            else
                throw new Exception("Invalid log output");
        }
        else
        {
            lock (lockFileStream)
            {
                // Flush File Data on Previously Opened File Stream
                if (fileStream != null)
                {
                    while (fileData.TryDequeue(out byte[]? data))
                        fileStream.Write(data, 0, data.Length);

                    fileStream.Dispose();
                }

                string? dir = Path.GetDirectoryName(output);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                fileStream = new FileStream(output, config.Append ? FileMode.Append : FileMode.Create, FileAccess.Write);
                if (lastOutput != ":file")
                {
                    Output = FilePtr;
                    lastOutput = ":file";
                }
            }
        }
    }

    static nint bufferLogLine;
    static int  printPrefix;
    static int* printPrefixPtr;

    static LogHandler logGlobal = null!;
    //static av_log_set_callback_callback? LogClbkDlg = null;

    internal static void SetAVLog()
    {
        FFmpegLog.SetLogLevel(config.FFmpegLogLevel);

        if (config.FFmpegLogLevel == LogLevel.Quiet)
            FFmpegLog.UnSetCallback();
        //else if (false && config.Output == ":console") // (we loose parents*) LogClbkDlg = av_log_default_callback; // Consider using default to print to console (colored output)
        //    LogClbkDlg = av_log_default_callback;
        else
            FFmpegLog.SetCallback(LogClbk);
            //LogClbkDlg = LogClbk;

        //av_log_set_callback(LogClbkDlg);
        
        //av_log_set_flags((int)LogFlags.PrintLevel); // TBR
    }

    internal static void LogClbk(void* avcl, LogLevel level, byte* format, byte* vl)
    {
        /* NOTES:
         * av_log_format_line/2 adds colors and can't customize the format (possible use a c# printf?) var objects = va_list_Helper.VaListToArray(format, vl);
         * 
         * thread safety issues (1 to avoid allocations of the buffer
         * Buffer will be by line? probably should be smaller
         * Fast way for FileStream/Stream use byte* pointer directly and just find the length?
         * direct utf8 write*
         */

        if ((int)level > (int)config.FFmpegLogLevel)
            return;

        string className = "", parentClassName = "";
        if (/*cfg.PrintPrefix &&*/ avcl != null)
        {
            AVClass* avc = *(AVClass**)avcl;
            if (avc != null)
            {
                if (avc->parent_log_context_offset != 0)
                {
                    AVClass** parent = *(AVClass ***) (((byte*) avcl) + avc->parent_log_context_offset);
                    if (parent != null && *parent != null)  
                    {
                        var ItemNameP = GetDelegateForFunctionPointer<AVClass_item_name>((*parent)->item_name.Pointer);
                        parentClassName = $"[{ItemNameP(parent)}] ";
                    }
                }
                var ItemName = GetDelegateForFunctionPointer<AVClass_item_name>(avc->item_name.Pointer);
                className = $"[{ItemName(avcl)}] ";
            }
        }

        LogHandler? log = (LogHandler?) Thread.GetData(Thread.GetNamedDataSlot($"log{Environment.CurrentManagedThreadId}"));

        if (log == null)
            log = logGlobal;

        if (!log.included)
            return;

        // lock is for (log.ffmpegPrevMsg) which is normally will be accessed from the same thread (but we can't be sure)
        // lock will not ensure single access to the bufferLogLine (TBR)
        lock (log)
        {
            printPrefix = 0;
            av_log_format_line2(avcl, level, format, vl, (byte*)bufferLogLine, 1024, printPrefixPtr);

            string msg = GetString(bufferLogLine)!;
            
            // TBR: multiple lines*
            if (msg.EndsWith('\n'))
            {
                WriteN($"{log.Prefix}{log.FFmpegPrefix}{parentClassName}{className}{log.FFmpegPrevMsg}{msg}", level);
                log.FFmpegPrevMsg = "";
            }
            else
                log.FFmpegPrevMsg += msg;
        }
    }

    static void DebugPtr(string msg) => Debug.Write(msg);
    static void DevNullPtr(string msg) { }
    static void FilePtr(string msg)
    {
        fileData.Enqueue(Encoding.UTF8.GetBytes(msg));

        if (!fileTaskRunning && fileData.Count > config.CachedLines)
            FlushFileData();
    }

    static void FlushFileData()
    {
        fileTaskRunning = true;

        Task.Run(() =>
        {
            lock (lockFileStream)
            {
                while (fileData.TryDequeue(out byte[]? data))
                    fileStream!.Write(data, 0, data.Length);
                
                fileStream!.Flush();
            }
            
            fileTaskRunning = false;
        });
    }

    /// <summary>
    /// Forces cached file data to be written to the file
    /// </summary>
    public static void ForceFlush()
    {
        if (!fileTaskRunning && fileStream != null)
            FlushFileData();
    }

    internal static void Write(string msg, LogLevel logLevel)
        { if (logLevel <= config.LogLevel) WriteN(msg, logLevel); }

    internal static void WriteLine(string msg, LogLevel logLevel)
        { if (logLevel <= config.LogLevel) WriteLineN(msg, logLevel); }

    internal static void WriteLineN(string msg, LogLevel logLevel)
        => WriteN(msg + "\r\n", logLevel);

    internal static void WriteN(string msg, LogLevel logLevel)
        => Output($"{DateTime.Now.ToString(config.DateTimeFormat)}{logLevels[logLevel]}{msg}");
}
