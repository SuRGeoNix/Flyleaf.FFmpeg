namespace Flyleaf.FFmpeg;

public unsafe static class ImageUtils
{
    // https://ffmpeg.org/doxygen/trunk/group__lavu__picture.html

    //public static int_array4 GetLineSizes(AVPixelFormat format, int width) // return error?*
    //{
    //    int_array4 linesizes = new();
    //    av_image_fill_linesizes(ref linesizes, format, width);
    //    return linesizes;
    //}

    //public static int GetPlaneSizes(ref ulong_array4 planeSizes, AVPixelFormat format, int height, int_array4 linesizes)
    //{
    //    nint_array4 linesizesLong = new();
    //    for (int i = 0; i < int_array4.Size; i++)
    //        linesizesLong[i] = linesizes[i];

    //    return FillPlaneSizes(ref planeSizes, format, height, linesizesLong);
    //}

    //public static int GetPlaneSizes(ref ulong_array4 planeSizes, AVPixelFormat format, int width, int height)
    //{
    //    int_array4 linesizes = new();
    //    int ret = FillLinesizes(ref linesizes, format, width);

    //    return ret < 0 ? ret : GetPlaneSizes(ref planeSizes, format, height, linesizes);
    //}

    //public static (int success, List<ulong> planeSizes) GetPlaneSizes(AVPixelFormat format, int width, int height)
    //{
    //    List<ulong> planeSizesList = [];
    //    ulong_array4 planeSizes = new();
    //    int ret = GetPlaneSizes(ref planeSizes, format, width, height);
    //    if (ret < 0)
    //        return (ret, []);

    //    for (int i = 0; i < ulong_array4.Size; i++)
    //        if (planeSizes[i] == 0)
    //            break;
    //        else
    //            planeSizesList.Add(planeSizes[i]);

    //    return (ret, planeSizesList);
    //}

    //public static int FillLinesizes(ref int_array4 linesizes, AVPixelFormat format, int width)
    //    => av_image_fill_linesizes(ref linesizes, format, width);

    //public static int FillPointers(ref byte_ptrArray4 dataPtrs, byte* dataPtr, AVPixelFormat format, int height, int_array4 linesizes)
    //    => av_image_fill_pointers(ref dataPtrs, format, height, dataPtr, linesizes);

    //public static int FillPlaneSizes(ref ulong_array4 planeSizes, AVPixelFormat format, int height, nint_array4 linesizes)
    //    => av_image_fill_plane_sizes(ref planeSizes, format, height, linesizes);

    public static int CheckSampleAspectRatio(uint width, uint height, AVRational sampleAspectRatio)
        => av_image_check_sar(width, height, sampleAspectRatio);

    public static int CheckSize(uint width, uint height)
        => av_image_check_size(width, height, (int)LogLevel.Quiet, null);

    public static int CheckSize(AVPixelFormat format, uint width, uint height, long maxPixels)
        => av_image_check_size2(width, height, maxPixels, format, (int)LogLevel.Quiet, null);

    public static int FillLinesizes(int* linesizes, AVPixelFormat format, int width)
        => av_image_fill_linesizes(linesizes, format, width);

    public static int FillPointers(byte** dataPtrs, byte* dataPtr, AVPixelFormat format, int height, int* linesizes)
        => av_image_fill_pointers(dataPtrs, format, height, dataPtr, linesizes);

    public static int FillPlaneSizes(nuint* planeSizes, AVPixelFormat format, int height, nint* linesizes) // should be nint*
        => av_image_fill_plane_sizes(planeSizes, format, height, linesizes);

    public static int FillArrays(byte* buf, byte** data, int* linesizes, AVPixelFormat format, int width, int height, int align = 1)
        => av_image_fill_arrays(data, linesizes, buf, format, width, height, align);

    public static int FillColor(byte** data, nint* linesizes, AVPixelFormat format, int width, int height, uint* color)
        => av_image_fill_color(data, linesizes, format, color, width, height, 0); // flags unused

    public static int FillBlack(byte** data, nint* linesizes, AVPixelFormat format, int width, int height, AVColorRange range)
        => av_image_fill_black(data, linesizes, format, range, width, height);

    public static void CopyPlane(byte* srcData, int srcLinesize, byte* dstData, int dstLinesize, int byteWidth, int height)
        => av_image_copy_plane(dstData, dstLinesize, srcData, srcLinesize, byteWidth, height);

    public static void Copy(byte** srcData, int* srcLinesizes, byte** dstData, int* dstLinesizes, AVPixelFormat format, int width, int height)
        => av_image_copy(dstData, dstLinesizes, srcData, srcLinesizes, format, width, height); // TBR: if av_image_copy2 required (seems not)

    public static int CopyToBuffer(byte** srcData, int* srcLinesizes, byte* dstBuffer, int dstSize, AVPixelFormat format, int width, int height, int align = 1)
        => av_image_copy_to_buffer(dstBuffer, dstSize, srcData, srcLinesizes, format, width, height, align);

    /* Notes
Video
    Copy Plane
	    For Height/rows copies bytewidth size (and increases the pointers by linesize which must be greather than bytewidth)
	
    Copy
	    Does the same as Copy Plane for all planes
	
    Copy to Buffer (Raw)
	    Same as Copy but the destination is a single "Plane" / Data
	
    GetPlanesCount 		(PixelFormat) 								-> Number of Planes
    GetBufferSize 		(PixelFormat, Width, Height, Align) 		-> Total buffer size		(should be same as Total sum of FillPlaneSizes)
    FillLineSizes		(PixelFormat, Width)						-> Linesize for each Plane
    FillPlaneSizes		(PixelFormat, Height, Linesizes)			-> Byte Size of each Plane 	(linesize * h)
    FillPointers    	(PixelFormat, Height, Linesizes)    		-> Calcs PlaneSizes and then the pointers based on the input dataPtr
    FillArrays      	(PixelFormat, Width, Height, Align) 		-> FillLineSizes + FillPointers (includes align)

Audio
    Copy				(Source, Destination, SampleFormat, Channels, Samples)

    GetPlanesCount		(SampleFormat, Channels)					-> IsPlanar ? Channels : 1;
    GetBufferSize		(SampleFormat, Channels, Samples, Align)	-> Total buffer size
    FillArrays			(SampleFormat, Channels, Samples, Align)

    GetBytesPerSample	(SampleFormat)								-> Byte Size of Each Sample
    IsPlanar			(SampleFormat)								-> Planar or Packed

Video
	Max 4 bufs / planes / linesizes
	Data Pointers / Planes = Based on Pixel Format

Audio
	Max 8 + Extended Bufs / Planes
	Linesize => always 1 array size
	Data Pointers / Planes = Planar Format ? Channels : 1
	
	Data / Bufs
	Extended Data / Bufs (+Number of extra Bufs)
	Extended Data will include the first 8 Data Pointers
	When there are no Extended Data then the Extended Data main Ptr will be equal to Data Ptr 
    */
}
