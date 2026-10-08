namespace Flyleaf.FFmpeg.Codec;

// TBR if required or just use AVSubtitle struct (should be RW and add view/disposable)
// avsubtitle_free just frees rects (and memsets struct to 0)


public unsafe class SubtitleFrame : PacketFrameSubs
{   // Start/End/Duration in Ms

    public SubtitleFormat   Format      { get => (SubtitleFormat)_ptr->format;  set => _ptr->format = (ushort)value; }      // This should be same for all based on avctx->codec_descriptor->props & AV_CODEC_PROP_BITMAP_SUB | AV_CODEC_PROP_TEXT_SUB
    public override long    Pts         { get => _ptr->pts;                     set => _ptr->pts = value; }                 // in codec->pkt_timebase (TBR: if it can be anything than ms)
    public uint             StartTime   { get => _ptr->start_display_time;      set => _ptr->start_display_time = value; }
    public uint             EndTime     { get => _ptr->end_display_time;        set => _ptr->end_display_time = value; }
    public AVSubtitleRect** Rects       => _ptr->rects;
    public uint             RectsNum    => _ptr->num_rects;
    public long             Duration    => _ptr->end_display_time - _ptr->start_display_time;
    public bool             Disposed    => _ptr == null;

    public readonly AVSubtitle* _ptr = (AVSubtitle*)av_mallocz((nuint)sizeof(AVSubtitle));

    public static implicit operator AVSubtitle*(SubtitleFrame frame)
        => frame._ptr;

    // TODO: copy fftools/ffmpeg_dec.c (copy_av_subtitle)

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

    public override void Dispose()
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

    public override string GetDump(AVRational timebase)
    {
        string pts, Pts, dur, Dur;

        if (_ptr->pts != NoTs)
        {
            pts = McsToTimeMini(av_rescale_q(_ptr->pts, timebase, TIME_BASE_Q)); // DoubleToTime(pkt->pts * av_q2d(Stream.Timebase));
            Pts = _ptr->pts.ToString();
        }
        else
        {
            pts = Pts = "-";
        }

        if (Duration > 0)
        {
            dur = McsToTimeMini(av_rescale_q(Duration, timebase, TIME_BASE_Q)); //DoubleToTime(pkt->duration * av_q2d(Stream.Timebase));
            Dur = Duration.ToString();
        }
        else
        {
            dur = Dur = "-";
        }
        
        return $"pts: {pts + " (" + Pts + ")",-25},dur: {dur + " (" + Dur + ")",-20}";
    }
    #endregion
}
