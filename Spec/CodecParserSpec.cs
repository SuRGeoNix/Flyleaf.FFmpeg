namespace Flyleaf.FFmpeg.Spec;

public unsafe sealed partial class CodecParserSpec
{
    public List<AVCodecID>  CodecIds    { get; } = [];

    public readonly AVCodecParser* _ptr;

    public CodecParserSpec(AVCodecParser* parser)
    {
        _ptr = parser;
        for (int i = 0; i < 7; i++)
        {
            if (_ptr->codec_ids[i] == 0)
                break;
            
            var codecId = (AVCodecID)_ptr->codec_ids[i];

            #if DEBUG
            if (CodecParserSpecById.ContainsKey(codecId))
                throw new Exception("CodecId with multiple parsers currently not allowed");
            #endif

            CodecParserSpecById[codecId] = this;
            //AddToDicList(CodecParserSpecsById, codecId, this);
            CodecIds.Add(codecId);
        }
    }

    //internal static Dictionary<AVCodecID, List<CodecParserSpec>>    CodecParserSpecsById = [];
    internal static Dictionary<AVCodecID, CodecParserSpec>    CodecParserSpecById = [];
    public static readonly List<CodecParserSpec> CodecParserSpecs = [];

    internal static void FillCodecParserSpecs()
    {
        AVCodecParser* cur;
        void* opaque = null;
        while ((cur = av_parser_iterate(ref opaque)) != null)
        {
            var spec = new CodecParserSpec(cur);
            CodecParserSpecs.Add(spec);
        }
    }
}
