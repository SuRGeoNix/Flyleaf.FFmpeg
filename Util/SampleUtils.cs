namespace Flyleaf.FFmpeg;

public unsafe static class SampleUtils
{
    public static (int success, nint[] dataPointers) SamplesAllocate(AVSampleFormat format, int channels, int samples, bool align = true)
    {
        int linesize, ret;
        int planes =  av_sample_fmt_is_planar(format) != 0 ? channels : 1;
        Span<nint> dataPtrs = stackalloc nint[planes];

        fixed(nint* dataPtrPtr = dataPtrs)
            ret = av_samples_alloc((byte**)dataPtrPtr, &linesize, channels, samples, format, align ? 0 : 1);

        return (ret, dataPtrs.ToArray());
    }

    // Sames as Allocate but allocates also the data[planes] size for pointers this will require to (comes from av_calloc) av_freep(***) as well
    public static int AllocateArraysAndSamples(byte*** data, int* linesize, AVSampleFormat format, int channels, int samples, bool align = true)
        => av_samples_alloc_array_and_samples(data, linesize, channels, samples, format, align ? 0 : 1);

    // Unsafe: this might exceed the array limits (audio can have more than 8 channels* data[8] + extended_data[X])
    public static int SamplesAllocate(ref byte_ptrArray8 data, ref int_array8 linesize, AVSampleFormat format, int channels, int samples, bool align = true)
    {
        fixed(byte_ptrArray8* dataPtr = &data)
            fixed(int_array8* linesizePtr = &linesize)
                return av_samples_alloc((byte**)dataPtr, (int*)linesizePtr, channels, samples, format, align ? 0 : 1);
    }

    // from single raw byte data to plane pointers (Fill Planes/Linesize?)
    public static int FillArrays(byte* buf, byte** data, int* linesize, AVSampleFormat format, int channels, int samples, bool align = true)
        => av_samples_fill_arrays(data, linesize, buf, channels, samples, format, align ? 0 : 1);

    public static int FillSilence(byte** data, int offset, AVSampleFormat format, int channels, int samples)
        => av_samples_set_silence(data, offset, samples, channels, format);

    public static int SamplesCopy(byte** src, byte** dst, AVSampleFormat format, int channels, int samples, int srcOffset = 0, int dstOffset = 0)
        => av_samples_copy(dst, src, dstOffset, srcOffset, samples, channels, format);
}
