namespace Flyleaf.FFmpeg.Codec;

// TBR if required or just use AVSubtitle struct (should be RW and add view/disposable)
// avsubtitle_free just frees rects (and memsets struct to 0)


public unsafe class SubtitleFrame : IDisposable
{
    public SubtitleFormat       Format                  { get => (SubtitleFormat)_ptr->format;  set => _ptr->format = (ushort)value; }      // This should be same for all based on avctx->codec_descriptor->props & AV_CODEC_PROP_BITMAP_SUB | AV_CODEC_PROP_TEXT_SUB
    public long                 Pts                     { get => _ptr->pts;                     set => _ptr->pts = value; }                 // in codec->pkt_timebase (TBR: if it can be anything than ms)
    public uint                 StartTimeMs             { get => _ptr->start_display_time;      set => _ptr->start_display_time = value; }
    public uint                 EndTimeMs               { get => _ptr->end_display_time;        set => _ptr->end_display_time = value; }
    public AVSubtitleRect**     Rects                   => _ptr->rects;
    public uint                 RectsNum                => _ptr->num_rects;

    public long                 DurationMs              => _ptr->end_display_time - _ptr->start_display_time;
    public bool                 Disposed                => _ptr == null;

    public readonly AVSubtitle* _ptr = (AVSubtitle*)av_mallocz((nuint)sizeof(AVSubtitle));

    public static implicit operator AVSubtitle*(SubtitleFrame frame)
        => frame._ptr;

    //public void Reset()
    //    => avsubtitle_free(_ptr);

    public void Reset()
    {
        if (_ptr->rects == null)
            return;

        for (int i = 0; i < _ptr->num_rects; i++)
        {
            var rect    = _ptr->rects[i];
            var dataPtrs= new Span<nint>(&rect->data, 4);

            for (int l = 0; l < dataPtrs.Length; l++)
                fixed(void* ptr = &dataPtrs[l])
                    av_freep(ptr);
            av_freep(&rect->text);
            av_freep(&rect->ass);
            av_freep(&_ptr->rects[i]);
        }

        av_freep(&_ptr->rects);
    }
    #region Disposal
    static System.Reflection.FieldInfo ptrField = typeof(SubtitleFrame).GetField(nameof(_ptr))!;
    ~SubtitleFrame()
    {
        if (!Disposed)
            Free();
    }

    public void Dispose()
    { 
        if (!Disposed)
        {
            Free();
            ptrField.SetValue(this, null);
            GC.SuppressFinalize(this);
        }
    }

    void Free()
    {
        Reset();
        av_free(_ptr);
        //FreeHGlobal((IntPtr)_ptr);
    }
    #endregion
}
