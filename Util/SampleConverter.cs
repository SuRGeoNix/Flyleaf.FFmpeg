using System.Diagnostics.CodeAnalysis;

namespace Flyleaf.FFmpeg;

public unsafe class SampleConverter : IDisposable
{
    public FFmpegClass                  AVClass                 => FFmpegClass.Get(ctx)!;

    public required AVSampleFormat      SampleFormatIn          { get => AVClass.GetSampleFormat("isf").result;                     set => AVClass.Set("isf", value); }
    public required int                 SampleRateIn            { get => (int)AVClass.GetLong("isr").result;                        set => AVClass.Set("isr", value); }
    public required AVChannelLayout     ChannelLayoutIn         { get => AVClass.GetChannelLayout("ichl").result;                   set => AVClass.Set("ichl", value); }
    public required AVSampleFormat      SampleFormatOut         { get => AVClass.GetSampleFormat("osf").result;                     set => AVClass.Set("osf", value); }
    public required int                 SampleRateOut           { get => (int)AVClass.GetLong("osr").result;                        set => AVClass.Set("osr", value); }
    public required AVChannelLayout     ChannelLayoutOut        { get => AVClass.GetChannelLayout("ochl").result;                   set => AVClass.Set("ochl", value); }

    public AVChannelLayout              ChannelLayoutUsed       { get => AVClass.GetChannelLayout("uchl").result;                   set => AVClass.Set("uchl", value); }
    public SwrDitherType                DitherMethod            { get => (SwrDitherType)AVClass.GetLong("dither_method").result;    set => AVClass.Set("dither_method", value); }
    public double                       DitherScale             { get => AVClass.GetDouble("dither_scale").result;                  set => AVClass.Set("dither_scale", value); }
    public double                       MixLevelCenter          { get => AVClass.GetDouble("clev").result;                          set => AVClass.Set("clev", value); }
    public double                       MixLevelSurround        { get => AVClass.GetDouble("slev").result;                          set => AVClass.Set("slev", value); }
    public double                       MixLevelLFE             { get => AVClass.GetDouble("lfe_mix_level").result;                 set => AVClass.Set("lfe_mix_level", value); }
    public double                       RematrixVolume          { get => AVClass.GetDouble("rmvol").result;                         set => AVClass.Set("rmvol", value); }
    public double                       RematrixMaxVal          { get => AVClass.GetDouble("rematrix_maxval").result;               set => AVClass.Set("rematrix_maxval", value); }
    public bool                         ResampleFlag            { get => AVClass.GetLong("flags").result == SWR_FLAG_RESAMPLE;      set => AVClass.Set("flags", value ? SWR_FLAG_RESAMPLE : 0); }
    public int                          FilterSize              { get => (int)AVClass.GetLong("filter_size").result;                set => AVClass.Set("filter_size", value); }
    public int                          PhaseShift              { get => (int)AVClass.GetLong("phase_shift").result;                set => AVClass.Set("phase_shift", value); }
    public bool                         LinearInterpolation     { get => AVClass.GetBool("linear_interp").result;                   set => AVClass.Set("linear_interp", value); }
    public bool                         ExactRational           { get => AVClass.GetBool("exact_rational").result;                  set => AVClass.Set("exact_rational", value); }
    public double                       CutOff                  { get => AVClass.GetDouble("cutoff").result;                        set => AVClass.Set("cutoff", value); }

    /* TBR: duplicate option in order to work with avconv */ 

    // Provide also this constructor for quick setup / init?
    [SetsRequiredMembers]
    public SampleConverter( AVChannelLayout srcChannelLayout, AVSampleFormat srcFormat, int srcSampleRate,
                            AVChannelLayout dstChannelLayout, AVSampleFormat dstFormat, int dstSampleRate)
    {
        new FFmpegResult(swr_alloc_set_opts2(ref ctx, &dstChannelLayout, dstFormat, dstSampleRate, &srcChannelLayout, srcFormat, srcSampleRate, 0, null)).ThrowOnFailure();
        new FFmpegResult(swr_init(ctx)).ThrowOnFailure();
    }

    public SampleConverter()
        => ctx = swr_alloc();

    public FFmpegResult InitContext()
        => new(swr_init(ctx));

    public bool IsInitialized()
        => swr_is_initialized(ctx) != 0;

    public long GetDelay(int sourceSampleRate)
        => swr_get_delay(ctx, sourceSampleRate);

    public long GetOutSamples(int samplesIn)
        => swr_get_out_samples(ctx, samplesIn);

    public long GetNextPts(long pts)
        => swr_next_pts(ctx, pts);

    public FFmpegResult SetChannelMapping(int* channelMap)
        => new(swr_set_channel_mapping(ctx, channelMap));

    public FFmpegResult SetCompensation(int sampleDelta, int compensationDistance)
        => new(swr_set_compensation(ctx, sampleDelta, compensationDistance));

    public FFmpegResult SetMatrix(double* matrix, int stride)
        => new(swr_set_matrix(ctx, matrix, stride));

    // TODO return result instead of failure? (no tuple with double*)
    public static double* BuildMatrix(AVChannelLayout channelLayoutIn, AVChannelLayout channelLayoutOut, double mixLevelCenter, double mixLevelSurround, double mixLevelLFE, double maxVal, double rematrixVoalume, AVMatrixEncoding matrixEncoding, nint stride)
    {
        double* matrix = null;
        new FFmpegResult(swr_build_matrix2(&channelLayoutIn, &channelLayoutOut, mixLevelCenter, mixLevelSurround, mixLevelLFE, maxVal, rematrixVoalume, matrix, stride, matrixEncoding, null)).ThrowOnFailure();
        return matrix;
    }

    public FFmpegResult DropOutput(int count)
        => new(swr_drop_output(ctx, count));

    public FFmpegResult InjectSilence(int count)
        => new(swr_inject_silence(ctx, count));

    public FFmpegResult Convert(byte_ptrArray8 srcData, int srcCount, byte_ptrArray8 dstData, int dstCount)
        => new(swr_convert(ctx, (byte**)&dstData, dstCount, (byte**)&srcData, srcCount));

    public FFmpegResult Convert(AudioFrameBase src, AudioFrameBase dst)
        => new(swr_convert_frame(ctx, dst, src));

    public FFmpegResult Drain(byte_ptrArray8 dstData, int dstCount)
        => new(swr_convert(ctx, (byte**)&dstData, dstCount, null, 0));

    #region Disposal
    public bool Disposed => ctx == null;

    public static implicit operator SwrContext*(SampleConverter ctx)
        => ctx.ctx;

    SwrContext* ctx;

    ~SampleConverter()
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

    protected void Free() // TBR: Can be closed and re-used
        => swr_free(ref ctx);
    #endregion
}
