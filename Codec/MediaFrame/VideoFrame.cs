namespace Flyleaf.FFmpeg.Codec;

public unsafe abstract class VideoFrameBase : FrameBase
{
    public AVRational           SampleAspectRatio       { get => _ptr->sample_aspect_ratio;     set => _ptr->sample_aspect_ratio = value;}

    public AVColorPrimaries     ColorPrimaries          { get => _ptr->color_primaries;         set => _ptr->color_primaries= value; }
    public AVColorTransferCharacteristic
                                ColorTransfer           { get => _ptr->color_trc;               set => _ptr->color_trc      = value; }
    public AVColorSpace         ColorSpace              { get => _ptr->colorspace;              set => _ptr->colorspace     = value; }
    public AVColorRange         ColorRange              { get => _ptr->color_range;             set => _ptr->color_range    = value; }
    public AVChromaLocation     ChromaLocation          { get => _ptr->chroma_location;         set => _ptr->chroma_location= value; }
    public AVPixelFormat        PixelFormat             { get => (AVPixelFormat)_ptr->format;   set => _ptr->format         = (int)value; }
    public int                  Width                   { get => _ptr->width;                   set => _ptr->width          = value; }
    public int                  Height                  { get => _ptr->height;                  set => _ptr->height         = value; }

    public AVPictureType        PictureType             { get => _ptr->pict_type;               set => _ptr->pict_type      = value; } // V only?
    public int                  Quality                 { get => _ptr->quality;                 set => _ptr->quality        = value; } // TBR: 1(good)-FF_LAMBDA_MAX(bad) (percentage?) V only?

    public int                  CropTop                 { get => (int)_ptr->crop_top;           set => _ptr->crop_top       = (nuint)value; }
    public int                  CropLeft                { get => (int)_ptr->crop_left;          set => _ptr->crop_left      = (nuint)value; }
    public int                  CropRight               { get => (int)_ptr->crop_right;         set => _ptr->crop_right     = (nuint)value; }
    public int                  CropBottom              { get => (int)_ptr->crop_bottom;        set => _ptr->crop_bottom    = (nuint)value; }
    public bool                 HasCrop                 => _ptr->crop_top !=0 || _ptr->crop_left != 0 || _ptr->crop_bottom != 0 || _ptr->crop_right != 0;

    public bool                 IsHWFrame               => _ptr->hw_frames_ctx != null;
    public HWFramesContextView? HWFramesContext         => _ptr->hw_frames_ctx == null ? null : new(_ptr->hw_frames_ctx);

    // HW Frames Helpers (inherit? DXVA2Frame/D3D11VideoFrame/D3D12VideoFrame)
    public IDirect3DSurface9*   D3D9Surface             { get => (IDirect3DSurface9*)   _ptr->data[3];  set => _ptr->data[3] = (nint)value; }
    public ID3D11Texture2D*     D3D11Texture            { get => (ID3D11Texture2D*)     _ptr->data[0];  set => _ptr->data[0] = (nint)value; }
    public int                  D3D11TextureIndex       { get => (int)                  _ptr->data[1];  set => _ptr->data[1] = value; }
    public AVD3D12VAFrame*      D3D12Frame              { get => (AVD3D12VAFrame*)      _ptr->data[0];  set => _ptr->data[0] = (nint)value; }

    protected VideoFrameBase() : base() { }
    protected VideoFrameBase(AVFrame* ptr) : base(ptr) { }

    public FFmpegResult ApplyCropping(bool aligned = true)
        => new(av_frame_apply_cropping(_ptr, aligned ? 0 : 1)); // AV_FRAME_CROP_UNALIGNED     = 1 << 0 (frame.h)

    public FFmpegResult CopyPropertiesTo(VideoFrameBase frame)
        => base.CopyPropertiesTo(frame);

    public VideoFrame Clone()
        => new(CloneRaw());

    public FFmpegResult Ref(VideoFrameBase frame)
        => base.Ref(frame);

    public void MoveRef(VideoFrameBase frame)
        => base.MoveRef(frame);

    public int GetBufferSize(int align = 1)
        => PixelFormat.GetBufferSize(Width, Height, align);

    public FFmpegResult TransferTo(VideoFrameBase frame) // Consider CopyPropertiesTo on success?
        => new(av_hwframe_transfer_data(frame, this, 0)); // flags unused

    public FFmpegResult Map(VideoFrameBase frame, AVHWframeMap flags = AVHWframeMap.None) // Consider CopyPropertiesTo / or w/h only on success?
        => new(av_hwframe_map(frame._ptr, _ptr, flags));

    public byte[] ToRawImage(int align = 1)
    {
        int ret;
        var buffer = new byte[GetBufferSize()];
        fixed(byte* bufferPtr = buffer)
            ret = ImageUtils.CopyToBuffer((byte**)&_ptr->data, (int*)&_ptr->linesize, bufferPtr, buffer.Length, PixelFormat, Width, Height, align);
            //ret = av_image_copy_to_buffer(bufferPtr, buffer.Length, (byte_ptrArray4)Data, (int_array4)Linesize, PixelFormat, Width, Height, align);

        return buffer;
    }

    public FFmpegData ToRawImage2(int align = 1)
    {
        int size = GetBufferSize();
        var data = new FFmpegData(size, false);

        _ = ImageUtils.CopyToBuffer((byte**)&_ptr->data, (int*)&_ptr->linesize, data.Pointer, size, PixelFormat, Width, Height, align);
        //_ = av_image_copy_to_buffer(data.Pointer, size, (byte_ptrArray4)Data, (int_array4)Linesize, PixelFormat, Width, Height, align);

        return data;
    }
}

public unsafe sealed class VideoFrameView(AVFrame* ptr) : VideoFrameBase(ptr) { }

public unsafe sealed class VideoFrame : VideoFrameBase, IDisposable
{
    public VideoFrame() : base() { }
    public VideoFrame(int width, int height, AVPixelFormat pixelFormat, bool initBuffer = true, int align = 0) : this()
    {
        Width       = width;
        Height      = height;
        PixelFormat = pixelFormat;
        if (initBuffer)
            InitBuffer(align);          // same as av_image_fill_arrays?
    }
    public VideoFrame(AVFrame* ptr) : base(ptr) { }

    // Fill frame from raw data/bytes (this is similar to rawdecoder without using palletes / aligns ... tbr) - usually using + AV_INPUT_BUFFER_PADDING_SIZE/64 alignment
    //public VideoFrame(int width, int height, AVPixelFormat pixelFormat, FFmpegData data, bool refcounted = false) : this(width, height, pixelFormat, false)
    //{
    //    byte* curDataPtr;

    //    if (refcounted)
    //    {
    //        _ptr->buf[0] = (nint) av_buffer_create(data.Pointer, (nuint)data.Size, DefaultBufferFreeDlgt, null, 0);
    //        curDataPtr = ((AVBufferRef*)_ptr->buf[0])->data;
    //    }
    //    else
    //        curDataPtr = data.Pointer; // probably wrong (shoul

    //    av_image_fill_linesizes((int*)&_ptr->linesize, PixelFormat, Width).ThrowFFmpegIfError();
    //    av_image_fill_pointers((byte**)&_ptr->data, PixelFormat, Height, curDataPtr, (int*)&_ptr->linesize).ThrowFFmpegIfError();
    //}

    #region Disposal
    ~VideoFrame()
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
    {
        fixed(AVFrame** _ptrPtr = &_ptr)
            av_frame_free(_ptrPtr);
    }
    #endregion
}
