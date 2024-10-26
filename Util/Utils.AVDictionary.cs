namespace Flyleaf.FFmpeg;

public unsafe static partial class Utils
{
    public static Dictionary<string, string>? AVDictToDict(AVDictionary* avdict)
    {
        if (avdict == null)
            return null;

        Dictionary<string, string> dict = new(StringComparer.OrdinalIgnoreCase); // Case Insensitive Dictionary
        AVDictionaryEntry* kv = null;

        while ((kv = av_dict_iterate(avdict, kv)) != null)
            dict.Add(GetString(kv->key)!, GetString(kv->value)!);
        
        return dict;
    }

    public static void AVDictToDict(Dictionary<string, string> dict, AVDictionary* avdict)
    {
        AVDictionaryEntry* kv = null;
        while ((kv = av_dict_iterate(avdict, kv)) != null)
            dict.Add(GetString(kv->key)!, GetString(kv->value)!);
    }

    public static AVDictionary* AVDictFromDict(Dictionary<string, string>? dict)
    {
        if (dict == null || dict.Count == 0)
            return null;
        
        AVDictionary* avdict = null;

        foreach(var kv in dict)
            _ = av_dict_set(&avdict, kv.Key, kv.Value, 0);

        return avdict;
    }

    public static void AVDictReplaceFromDict(Dictionary<string, string>? dict, AVDictionary** dictPtr)
    {
        AVDictFree(dictPtr);
        *dictPtr = dict == null ? null : AVDictFromDict(dict);
    }

    public static void AVDictFree(AVDictionary** dictPtr)
        => av_dict_free(dictPtr);
}
