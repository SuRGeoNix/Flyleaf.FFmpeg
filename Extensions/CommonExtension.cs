namespace Flyleaf.FFmpeg;

public unsafe static class CommonExtension
{
    public static string GetName(this AVChannelLayout chLayout)
        => Utils.GetName(&chLayout);

    public static AVPixFmtDescriptor* GetDescriptor(this AVPixelFormat format)
        => av_pix_fmt_desc_get(format);

    public static string GetName(this AVCodecID codecId) // normally this is not required as we use specs? (or if it is, consider using CodecSpecById)
        => avcodec_get_name(codecId);

    public static int GetBitsPerSample(this AVCodecID codecId)
        => av_get_bits_per_sample(codecId);

    public static AVMediaType GetMediaType(this AVCodecID codecId)
        => avcodec_get_type(codecId);

    public static int GetExactBitsPerSample(this AVCodecID codecId)
        => av_get_exact_bits_per_sample(codecId);

    public static string GetName(this AVMediaType type)
        => av_get_media_type_string(type);

    public static string GetName(this AVPixelFormat format)
        => av_get_pix_fmt_name(format);

    public static string GetName(this AVSampleFormat format)
        => av_get_sample_fmt_name(format);

    public static bool IsPlanar(this AVSampleFormat format)
        => av_sample_fmt_is_planar(format) != 0;

    public static AVSampleFormat GetPackedFormat(this AVSampleFormat format)
        => av_get_packed_sample_fmt(format);

    public static AVSampleFormat GetPlanarFormat(this AVSampleFormat format)
        => av_get_planar_sample_fmt(format);

    public static int GetBytesPerSample(this AVSampleFormat format)
        => av_get_bytes_per_sample(format);

    public static int GetPlanesCount(this AVPixelFormat format)
        => av_pix_fmt_count_planes(format);

    public static long GetBufferSize(this AVSampleFormat format, int channels, int samples, bool align = true, int* linesize = null)
        => av_samples_get_buffer_size(linesize, channels, samples, format, align ? 0 : 1);

    public static int GetBufferSize(this AVPixelFormat format, int width, int height, int align = 1) // This will not work with HWAccel Pixel Formats
        => av_image_get_buffer_size(format, width, height, align);
}
