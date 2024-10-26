namespace Flyleaf.FFmpeg;

public unsafe class AudioFifo : IDisposable
{
    public int      Size        => av_audio_fifo_size(_ptr);
    public int      Space       => av_audio_fifo_space(_ptr);
    public bool     Disposed    => _ptr == null;

    public readonly AVAudioFifo* _ptr;

    public AudioFifo(AVSampleFormat format, int channels, int samples)
        => _ptr = av_audio_fifo_alloc(format, channels, samples);

    public int ReAllocate(int samples)
        => av_audio_fifo_realloc(_ptr, samples);

    public void Reset()
        => av_audio_fifo_reset(_ptr);

    public int Drain(int samples)
        => av_audio_fifo_drain(_ptr, samples);

    public int Peek(byte** data, int samples)
        => av_audio_fifo_peek(_ptr, (void**)data, samples);

    public int Peek(byte** data, int samples, int offset)
        => av_audio_fifo_peek_at(_ptr, (void**)data, samples, offset);

    public int Read(byte** data, int samples)
        => av_audio_fifo_read(_ptr, (void**)data, samples);

    public int Write(byte** data, int samples)
        => av_audio_fifo_write(_ptr, (void**)data, samples);

    #region Disposal
    ~AudioFifo()
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

    static System.Reflection.FieldInfo ptrField = typeof(AudioFifo).GetField(nameof(_ptr))!;
    void Free()
    {
        av_audio_fifo_free(_ptr);
        ptrField.SetValue(this, null);
    }
    #endregion
}
