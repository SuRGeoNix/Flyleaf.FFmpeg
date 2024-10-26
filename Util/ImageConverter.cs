namespace Flyleaf.FFmpeg;

public unsafe class ImageConverter : IDisposable
{
    public FFmpegClass AVClass  => FFmpegClass.Get(ctx)!;

    public ImageConverter(AVPixelFormat srcFormat, int srcWidth, int srcHeight, AVPixelFormat dstFormat, int dstWidth, int dstHeight, SwsFlags flags)
    {
        ctx = sws_getContext(srcWidth, srcHeight, srcFormat, dstWidth, dstHeight, dstFormat, flags, null, null, null);
        if (ctx == null)
            throw new Exception("Failed to allocate SwsContext");
    }

    public ImageConverter() // requires Init
        => ctx = sws_alloc_context();

    public int InitContext()
        => sws_init_context(ctx, null, null);

    public int FillColorSpaceDetails(int_array4 srcSpace, bool srcFullRange, int_array4 dstSpace, bool dstFullRange, int brightness, int contrast, int saturation) // class?
        => sws_setColorspaceDetails(ctx, srcSpace, srcFullRange ? 1 : 0, dstSpace, dstFullRange ? 1 : 0, brightness, contrast, saturation);

    public (int success, int[] scrSpace, bool srcFullRange, int[] dstSpace, bool dstFullRange, int brightness, int contrast, int saturation) // class?
        GetColorSpaceDetails()
    {
        int* srcSpace       = null;
        int  srcFullRange   = default;
        int* dstSpace       = null;
        int  dstFullRange   = default;
        int  brightness     = default;
        int  contrast       = default;
        int  saturation     = default;
        
        int ret = sws_getColorspaceDetails(ctx, &srcSpace, &srcFullRange, &dstSpace, &dstFullRange, &brightness, &contrast, &saturation);
        
        return ret < 0 ?
            (ret, [], false, [], false, 0, 0, 0) : 
            (ret, new Span<int>(srcSpace, 4).ToArray(), srcFullRange != 0, new Span<int>(dstSpace, 4).ToArray(), dstFullRange != 0, brightness, contrast, saturation);
    }

    public int Convert(byte_ptrArray4 srcData, int_array4 srcLinesize, int srcSliceH, byte_ptrArray4 dstData, int_array4 dstLinesize, int srcSliceY = 0)
        => sws_scale(ctx, srcData.ToRawArray(), srcLinesize.ToArray(), srcSliceY, srcSliceH, dstData.ToRawArray(), dstLinesize.ToArray());

    public int Convert(byte_ptrArray8 srcData, int_array8 srcLinesize, int srcSliceH, byte_ptrArray8 dstData, int_array8 dstLinesize, int srcSliceY = 0)
        => sws_scale(ctx, srcData.ToRawArray(), srcLinesize.ToArray(), srcSliceY, srcSliceH, dstData.ToRawArray(), dstLinesize.ToArray());

    public int Convert(VideoFrameBase src, VideoFrameBase dst)
        => sws_scale_frame(ctx, dst, src);

    public static bool IsSupportedForInput(AVPixelFormat format)
        => sws_isSupportedInput(format) > 0
        ;
    public static bool IsSupportedForOutput(AVPixelFormat format)
        => sws_isSupportedOutput(format) > 0;

    public static int[] GetCoeffs(SwsCSFlags colorspace)
    {
        int* ret = sws_getCoefficients((int)colorspace);
        return ret == null ? [] : new Span<int>(ret, 4).ToArray();
    }
    
    #region Disposal
    public bool Disposed => ctx == null;

    public static implicit operator SwsContext*(ImageConverter ctx)
        => ctx.ctx;

    SwsContext* ctx;

    ~ImageConverter()
    {
        if (!Disposed)
            Free();
    }

    public void Dispose()
    { 
        if (!Disposed)
        {
            Free();
            GC.SuppressFinalize(this);
        }
    }

    void Free()
        { sws_freeContext(ctx); ctx = null; }
    #endregion
}
