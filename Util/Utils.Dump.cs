namespace Flyleaf.FFmpeg;

public unsafe static partial class Utils
{
    static string metaSpaces = new(' ',"[Metadata] ".Length);
    public static string DumpMetadata(Dictionary<string, string>? metadata, string? exclude = null/*, string header = "Metadata"*/) // make it more general and add list exlude?
    {
        if (metadata == null || metadata.Count == 0)
            return "";

        int maxLen = 0;
        foreach(var item in metadata)
            if (item.Key.Length > maxLen && item.Key != exclude)
                maxLen = item.Key.Length;

        string dump = "";
        int i = 1;
        foreach(var item in metadata)
        {
            if (item.Key == exclude)
            {
                i++;
                continue;
            }

            if (i == metadata.Count)
                dump += $"{item.Key.PadRight(maxLen)}: {item.Value}\r\n";
            else
                dump += $"{item.Key.PadRight(maxLen)}: {item.Value}\r\n\t{metaSpaces}";

            i++;
        }

        if (dump == "")
            return "";
        
        return $"\t[Metadata] {dump}";
    }

    static string optSpaces = new(' ',"[Options] ".Length);
    public static string DumpOptions(Dictionary<string, string> optsIn, Dictionary<string, string>? optsOut = null) // make it more general and add list exlude?
    {
        if (optsIn == null || optsIn.Count == 0)
            return "";

        if (optsOut == null)
            optsOut = [];

        int maxLen = 0;
        foreach(var item in optsIn)
            if (item.Key.Length > maxLen)
                maxLen = item.Key.Length;

        string dump = "";
        int i = 1;
        foreach(var item in optsIn)
        {
            string ignored = optsOut.ContainsKey(item.Key) ? " (Ignored)" : "";

            if (i == optsIn.Count)
                dump += $"{item.Key.PadRight(maxLen)}: {item.Value}{ignored}\r\n";
            else
                dump += $"{item.Key.PadRight(maxLen)}: {item.Value}{ignored}\r\n\t{optSpaces}";

            i++;
        }

        if (dump == "")
            return "";
        
        return $"\t[Options] {dump}";
    }
}
