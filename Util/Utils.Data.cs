namespace Flyleaf.FFmpeg;

public unsafe static partial class Utils
{
    internal static void SideDataCopy(AVPacketSideData* src, int srcCount, AVPacketSideData** dstPtr, int* dstCount)
    {
        if (srcCount <= 0)
            return;

        AVPacketSideData* dst = (AVPacketSideData*) av_calloc((nuint)srcCount, (nuint)sizeof(nint));

        for (int i = 0; i < srcCount; i++)
        {
            var cursrc = src[i];
            var curdst = &dst[i];
            curdst->data = (byte*) av_memdup(cursrc.data, cursrc.size); // should benchmark av_memdup with Span.CopyTo
            curdst->size = cursrc.size;
            curdst->type = cursrc.type;
        }
    }

    internal static FFmpegResult SideDataCopy(AVFrameSideData** src, int srcCount, AVFrameSideData*** dstPtr, int* dstCount, FrameSideDataFlags flags = FrameSideDataFlags.None)
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

    internal static void ExtraDataCopy(byte* src, int srcSize, byte** dstPtr, int* dstSize)
    {
        if (*dstPtr != null)
            av_free(*dstPtr);

        *dstSize    = srcSize;

        if (srcSize <= 0)
            return;

        *dstPtr     = (byte*) av_mallocz((nuint)(srcSize + AV_INPUT_BUFFER_PADDING_SIZE));
        var srcSpan = new ReadOnlySpan<byte>(src, srcSize);
        var dstSpan = new Span<byte>(*dstPtr, srcSize);
        srcSpan.CopyTo(dstSpan);
    }

    internal static void HeaderDataCopy(byte* src, int srcSize, byte** dstPtr, int* dstSize)
    {
        // TBR: currently ensures we write even 0 size (1 byte null terminated)

        if (*dstPtr != null)
            av_free(*dstPtr);

        *dstPtr     = (byte*) av_mallocz((nuint)(srcSize + 1)); /* ASS code assumes this buffer is null terminated so add extra byte. */
        *dstSize    = srcSize;

        if (srcSize <= 0)
            return;

        var srcSpan = new ReadOnlySpan<byte>(src, srcSize);
        var dstSpan = new Span<byte>(*dstPtr, srcSize);
        srcSpan.CopyTo(dstSpan);
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