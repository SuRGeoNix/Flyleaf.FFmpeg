namespace Flyleaf.FFmpeg;

public unsafe static partial class Utils
{
    // TBR: av_packet_side_data_from_frame / av_packet_side_data_to_frame (packet / frame side data objects to each other)

    public static void SideDataCopy(AVPacketSideData* src, int srcCount, AVPacketSideData** dstPtr, int* dstCount)
    {
        if (*dstPtr != null)
            av_packet_side_data_free(dstPtr, dstCount);

        if (srcCount <= 0)
            return;

        var dst = (AVPacketSideData*) av_calloc((nuint)srcCount, (nuint)sizeof(AVPacketSideData));

        if (dst == null)
            return;

        for (int i = 0; i < srcCount; i++)
        {
            var srcCur = &src[i];
            var dstCur = &dst[i];
            
            dstCur->type = srcCur->type;
            if (srcCur->data != null && srcCur->size > 0)
            {
                dstCur->size = srcCur->size;
                dstCur->data = (byte*) av_memdup(srcCur->data, srcCur->size);
            }
        }

        *dstPtr     = dst;
        *dstCount   = srcCount;
    }

    public static void SideDataCopy(List<PacketSideDataEntry> src, AVPacketSideData** dstPtr, int* dstCount)
    {
        if (*dstPtr != null)
            av_packet_side_data_free(dstPtr, dstCount);

        if (src.Count <= 0)
            return;

        var dst = (AVPacketSideData*) av_calloc((nuint)src.Count, (nuint)sizeof(AVPacketSideData));

        if (dst == null)
            return;

        for (int i = 0; i < src.Count; i++)
        {
            var srcCur = src[i];
            var dstCur = &dst[i];
            
            dstCur->type = srcCur.Type;
            if (srcCur.Data.Length > 0)
            {
                dstCur->size = (nuint)srcCur.Data.Length;
                fixed(byte* ptr = srcCur.Data)
                    dstCur->data = (byte*) av_memdup(ptr, dstCur->size);
            }
        }

        *dstPtr     = dst;
        *dstCount   = src.Count;
    }

    public static List<PacketSideDataEntry> SideDataGet(AVPacketSideData* src, int srcCount)
    {
        var data = new List<PacketSideDataEntry>(srcCount);

        for (int i = 0; i < srcCount; i++)
        {
            var srcCur = &src[i];

            data.Add(new()
            {
                Type = srcCur->type,
                Data = srcCur->size > int.MaxValue || srcCur->size == 0 || srcCur->data == null ? [] : new ReadOnlySpan<byte>(srcCur->data, (int)srcCur->size).ToArray()
            });
        }
        
        return data;
    }

    public static FFmpegResult SideDataCopy(AVFrameSideData** src, int srcCount, AVFrameSideData*** dstPtr, int* dstCount, FrameSideDataFlags flags = FrameSideDataFlags.None)
    {
        // Posible get side data descriptor and check props (eg. global)
        FFmpegResult ret;
        for (int i = 0; i < srcCount; i++)
            if (!(ret = new(av_frame_side_data_clone(dstPtr, dstCount, src[i], (uint)flags))).Success)
                return ret;
        
        return FFmpegResult.Default;
    }

    public static FFmpegResult FrameSideDataCloneEntry(AVFrameSideData* srcEntry, AVFrameSideData*** dstPtr, int* dstCount, FrameSideDataFlags flags = FrameSideDataFlags.None)
        => new(av_frame_side_data_clone(dstPtr, dstCount, srcEntry, (uint)flags));

    public static void ExtraDataCopy(byte* src, int srcSize, byte** dstPtr, int* dstSize)
    {
        if (*dstPtr != null)
            av_freep(dstPtr);

        if (src == null || srcSize <= 0)
        {
            *dstSize = 0;
            return;
        }

        *dstPtr = (byte*) av_mallocz((nuint)srcSize + AV_INPUT_BUFFER_PADDING_SIZE);
        new ReadOnlySpan<byte>(src, srcSize).CopyTo(new Span<byte>(*dstPtr, srcSize));
        *dstSize = srcSize;
    }

    public static void HeaderDataCopy(byte* src, int srcSize, byte** dstPtr, int* dstSize)
    {   // TBR: null terminated extra byte (required by ASS?)
        if (*dstPtr != null)
            av_freep(dstPtr);

        if (src == null || srcSize <= 0)
        {
            *dstSize = 0;
            *dstPtr = (byte*) av_mallocz(1);
            return;
        }
        
        *dstPtr = (byte*) av_mallocz((nuint)srcSize + 1);
        new ReadOnlySpan<byte>(src, srcSize).CopyTo(new Span<byte>(*dstPtr, srcSize));
        *dstSize = srcSize;
    }
}

// Have separate the disposable so we can 'define' which places need to be freed by ffmpeg and not by us
public unsafe class FFmpegData
{
    public byte*    Pointer { get; }
    public int      Size    { get; }
    
    public FFmpegData(byte* ptr, int length)
    {
        Pointer = ptr;
        Size = length;
    }
    public FFmpegData(int size, bool zero = false, bool owner = false) // by default false? normally we pass the data to ffmpeg and gets the ownership
    {
        Owner   = owner;
        Size    = size;
        Pointer = zero ? (byte*)av_mallocz((nuint)Size) : (byte*)av_malloc((nuint)Size);

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        if (!owner) GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    }

    public Span<byte> AsSpan()
        => new(Pointer, Size);

    public ReadOnlySpan<byte> AsReadOnlySpan()
        => new(Pointer, Size);

    void Free()
        => av_free(Pointer);

    public byte* TodoPassToFFmpeg()
    {
        if (!Owner)
            throw new Exception("Not owned data tried to pass ownage to ffmpeg");

        Owner = false;
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

        return Pointer;
    }

    public bool Owner       { get; private set; }
    public bool Disposed    => Pointer == null;

    ~FFmpegData()
    {
        if (!Disposed && Owner)
            Free();
    }
    public void Dispose()
    { 
        if (!Disposed && Owner)
        {
            Free();
            GC.SuppressFinalize(this);
        }
    }
}

public sealed class PacketSideDataEntry
{
    public AVPacketSideDataType Type { get; init; }
    public byte[]               Data { get; init; } = [];
}