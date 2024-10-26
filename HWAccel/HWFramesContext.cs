using Flyleaf.FFmpeg.Codec.Decode;

namespace Flyleaf.FFmpeg.HWAccel;

public unsafe abstract class HWFramesContextBase
{
    public FFmpegClass          AVClass             => FFmpegClass.Get(_ptr->data)!;
    public AVPixelFormat        HWPixelFormat       { get => _ctx->format;              set => _ctx->format = value; }
    public AVPixelFormat        SWPixelFormat       { get => _ctx->sw_format;           set => _ctx->sw_format = value; }
    public int                  Width               { get => _ctx->width;               set => _ctx->width = value; }
    public int                  Height              { get => _ctx->height;              set => _ctx->height = value; }
    public int                  InitialPoolSize     { get => _ctx->initial_pool_size;   set => _ctx->initial_pool_size = value; }

    public AVBufferPool*        AVBufferPool        => _ctx->pool;
    public bool                 Disposed            => _ptr == null;

    public readonly AVBufferRef* _ptr;
    public readonly AVHWFramesContext* _ctx; // VS will not understand/show those in the debugger eg. ((AVHWFramesContext*)_ptr->data)->format -  Using AVHWFramesContext* ctx => (AVHWFramesContext*)_ptr->data; will resolve the issue
    public readonly void* _hwctx;

    public static implicit operator AVBufferRef*(HWFramesContextBase? ctx)
        => ctx != null ? ctx._ptr : null;

    protected HWFramesContextBase(AVBufferRef* buffer)
    {
        _ptr    = buffer;
        _ctx    = (AVHWFramesContext*)_ptr->data;
        _hwctx  = _ctx->hwctx;
    }

    // Requires to set all properties
    public HWFramesContextBase(HWDeviceContext device)//, AVPixelFormat hwPixelFormat, int width, int height, int initialPoolsize = 0)
    {
        _ptr    = av_hwframe_ctx_alloc(device); // keeps ref
        _ctx    = (AVHWFramesContext*)_ptr->data;
        _hwctx  = _ctx->hwctx;
    }
    
    public HWFramesContextBase(VideoDecoder decoder, HWDeviceContext device, AVPixelFormat hwPixelFormat) // decoding-get_format only / requires init (check docs more notes) (must use this for decoding to set also the required initial_pool_size based on the current decoder)
    {
        new FFmpegResult(avcodec_get_hw_frames_parameters(decoder._ptr, device, hwPixelFormat, ref _ptr)).ThrowOnFailure();
        _ctx    = (AVHWFramesContext*)_ptr->data;
        _hwctx  = _ctx->hwctx;
    }
    
    public FFmpegResult AllocateHWFrame(VideoFrame frame)
        => new(av_hwframe_get_buffer(_ptr, frame._ptr, 0));

    public FFmpegResult AllocateHWFrame(AVFrame* frame)
        => new(av_hwframe_get_buffer(_ptr, frame, 0)); // unused flags

    public FFmpegResult InitFrames()
        => new(av_hwframe_ctx_init(_ptr));

    public List<AVPixelFormat>? SupportedTransferFrom()
    {
        AVPixelFormat* formats;
        _ = av_hwframe_transfer_get_formats(_ptr, AVHWFrameTransferDirection.From, &formats, 0);
        return GetPixelFormats(formats);
    }

    public List<AVPixelFormat>? SupportedTransferTo()
    {
        AVPixelFormat* formats;
        _ = av_hwframe_transfer_get_formats(_ptr, AVHWFrameTransferDirection.To, &formats, 0);
        return GetPixelFormats(formats);
    }

    public AVBufferRef* RefRaw()
        => av_buffer_ref(_ptr);

    public HWFramesContext Ref()
        => new(av_buffer_ref(_ptr));
}

public unsafe class HWFramesContextView(AVBufferRef* ptr) : HWFramesContextBase(ptr) { }

public unsafe class HWFramesContext : HWFramesContextBase, IDisposable
{
    public HWFramesContext(HWDeviceContext device) : base(device) { }

    public HWFramesContext(VideoDecoder decoder, HWDeviceContext device, AVPixelFormat hwPixelFormat) : base(decoder, device, hwPixelFormat) { }
    internal HWFramesContext(AVBufferRef* buffer) : base(buffer) { }

    #region Disposal
    ~HWFramesContext()
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
        fixed(AVBufferRef** _ptrPtr = &_ptr)
            av_buffer_unref(_ptrPtr);
    }
    #endregion
}

// TBR: This is for owner/disposal (possible dublicate for views)
public unsafe class DXVA2FramesContext : HWFramesContext
{
    public CULong                   Surfacetype             => _hwctx->surface_type;
    public int                      SurfaceCount            => _hwctx->nb_surfaces;
    public IDirect3DSurface9**      Surfaces                => _hwctx->surfaces;
    
    public new readonly AVDXVA2FramesContext* _hwctx;

    public DXVA2FramesContext(VideoDecoder decoder, DXVA2DeviceContext device) : base(decoder, device, AVPixelFormat.Dxva2Vld)
        => _hwctx = (AVDXVA2FramesContext*)_ctx->hwctx;
}

public unsafe class D3D11VAFramesContext : HWFramesContext
{
    public nint                     Texture                 { get => (nint)_hwctx->texture; set => _hwctx->texture = (ID3D11Texture2D*)value; } // this can be provided by the user before init
    public AVD3D11FrameDescriptor*  AVFrameDescriptor       { get => _hwctx->texture_infos; set => _hwctx->texture_infos = value; } // this is an array with indexes 
    public uint                     MiscFlags               { get => _hwctx->MiscFlags;     set => _hwctx->MiscFlags = value; }
    public uint                     BindFlags               { get => _hwctx->BindFlags;     set => _hwctx->BindFlags = value; }

    public new readonly AVD3D11VAFramesContext* _hwctx;

    public D3D11VAFramesContext(VideoDecoder decoder, D3D11VADeviceContext device) : base(decoder, device, AVPixelFormat.D3d11)
        => _hwctx = (AVD3D11VAFramesContext*)_ctx->hwctx;
}

public unsafe class D3D12VAFramesContext : HWFramesContext
{
    public DXGI_FORMAT              DXGIFormat              { get => _hwctx->format;        set => _hwctx->format = value; }
    public D3D12_RESOURCE_FLAGS     ResourceFlags           { get => _hwctx->flags;         set => _hwctx->flags = value; }

    public new readonly AVD3D12VAFramesContext* _hwctx;

    public D3D12VAFramesContext(VideoDecoder decoder, D3D12VADeviceContext device) : base(decoder, device, AVPixelFormat.D3d12)
        => _hwctx = (AVD3D12VAFramesContext*)_ctx->hwctx;
}