namespace Flyleaf.FFmpeg.HWAccel;
/* TODO
 * Fix struct types for vk / cl etc... (typedefs)
 * 

Cuda    * D | E | F     (no internal? from ffmpeg tool?)
Opencl  * F
Vulkan  * D | F
Dxva2   * D
D3d11va * D
D3d12va * D ? ?
Vaapi   * D E F         (no windows)
Videotoolbox * D E F    (apple only, no device?)
Qsv     (intel only)
 */

public unsafe abstract class HWDeviceContextBase
{
    // av_hwdevice_find_type_by_name (same as enum names lowercase)
    // av_hwdevice_iterate_types (this only test the compilation config)
    public FFmpegClass          AVClass             => FFmpegClass.Get(_ctx)!;
    public AVHWDeviceType       Type                => _ctx->type;
    public string               Name                => av_hwdevice_get_type_name(Type);
    public HWFramesConstraints? HWFramesConstraints => HWFramesConstraints.Get(this);

    public bool                 Disposed            => _ptr == null;

    public readonly AVBufferRef* _ptr;
    public readonly AVHWDeviceContext* _ctx;
    public readonly void* _hwctx;
    
    public static implicit operator AVBufferRef*(HWDeviceContextBase ctx)
        => ctx._ptr;

    protected HWDeviceContextBase(AVBufferRef* ptr)
    {
        _ptr    = ptr;
        _ctx    = (AVHWDeviceContext*)_ptr->data;
        _hwctx  = _ctx->hwctx;
    }

    protected HWDeviceContextBase(AVHWDeviceType type) // using this way we must set the device manually before init
    {
        _ptr    = av_hwdevice_ctx_alloc(type);
        _ctx    = (AVHWDeviceContext*)_ptr->data;
        _hwctx  = _ctx->hwctx;
    }

    // dont call init if use this (not adapterId auto required at least for dxva2) - flags unused
    protected HWDeviceContextBase(AVHWDeviceType type, string? adapterId, Dictionary<string, string>? opts = null) // (decoding only?) how to separate alloc with create in constructor?
    {
        var avopts  = AVDictFromDict(opts);
        var ret     = new FFmpegResult(av_hwdevice_ctx_create(ref _ptr, type, adapterId, avopts, 0));
        if (avopts != null)
            AVDictFree(&avopts);

        ret.ThrowOnFailure();

        _ctx    = (AVHWDeviceContext*)_ptr->data;
        _hwctx  = _ctx->hwctx;
    }

    public HWDeviceContextBase(HWDeviceContext sourceDevice, AVHWDeviceType type, Dictionary<string, string>? opts = null) // store srcDevice?
    {
        var avopts  = AVDictFromDict(opts);
        var ret     = new FFmpegResult(av_hwdevice_ctx_create_derived_opts(ref _ptr, type, sourceDevice._ptr, avopts, 0));
        if (avopts != null)
            AVDictFree(&avopts);
        
        ret.ThrowOnFailure();

        _ctx    = (AVHWDeviceContext*)_ptr->data;
        _hwctx  = _ctx->hwctx;
    }

    public FFmpegResult InitDevice()
        => new(av_hwdevice_ctx_init(_ptr));

    public AVBufferRef* RefRaw()
        => av_buffer_ref(_ptr);

    public HWDeviceContext Ref()
        => new(av_buffer_ref(_ptr));
}

public unsafe class HWDeviceContextView(AVBufferRef* ptr) : HWDeviceContextBase(ptr) { }

public unsafe class HWDeviceContext : HWDeviceContextBase, IDisposable
{
    internal HWDeviceContext(AVBufferRef* ptr) : base(ptr) { }
    protected HWDeviceContext(AVHWDeviceType type) : base(type) { } 
    public HWDeviceContext(AVHWDeviceType type, string? adapterId, Dictionary<string, string>? opts = null) : base(type, adapterId, opts) { }
    public HWDeviceContext(HWDeviceContext sourceDevice, AVHWDeviceType type, Dictionary<string, string>? opts = null) : base(sourceDevice, type, opts) { }

    #region Disposal
    ~HWDeviceContext()
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

// TODO HWDevice Specific
// caller should add ref to device? / locks*? (should pass an already created d3d11 mutex to underlying or use free *lock?)

public unsafe class DXVA2DeviceContext : HWDeviceContext
{
    public nint Device              => (nint)_hwctx->devmgr;

    public new readonly AVDXVA2DeviceContext* _hwctx;

    public DXVA2DeviceContext(nint device) : base(AVHWDeviceType.Dxva2)
    {
        _hwctx          = (AVDXVA2DeviceContext*)_ctx->hwctx;
        _hwctx->devmgr  = (IDirect3DDeviceManager9*) device;
    }

    public DXVA2DeviceContext(string? adapterId = null, Dictionary<string, string>? opts = null) : base(AVHWDeviceType.Dxva2, adapterId, opts)
        => _hwctx = (AVDXVA2DeviceContext*)_ctx->hwctx;
}

public unsafe class D3D11VADeviceContext : HWDeviceContext
{
    public nint Device              => (nint)_hwctx->device;
    public nint DeviceContext       => (nint)_hwctx->device_context;
    public nint VideoDevice         => (nint)_hwctx->video_device;
    public nint VideoDeviceContext  => (nint)_hwctx->video_context;
    
    public new readonly AVD3D11VADeviceContext* _hwctx;

    public D3D11VADeviceContext(nint device) : base(AVHWDeviceType.D3d11va)
    {
        _hwctx          = (AVD3D11VADeviceContext*)_ctx->hwctx;
        _hwctx->device  = (ID3D11Device*) device;
    }

    public D3D11VADeviceContext(string? adapterId = null, Dictionary<string, string>? opts = null) : base(AVHWDeviceType.D3d11va, adapterId, opts)
        => _hwctx = (AVD3D11VADeviceContext*)_ctx->hwctx;
}

public unsafe class D3D12VADeviceContext : HWDeviceContext
{
    public nint Device              => (nint)_hwctx->device;
    public nint VideoDevice         => (nint)_hwctx->video_device;
    
    public new readonly AVD3D12VADeviceContext* _hwctx;

    public D3D12VADeviceContext(nint device) : base(AVHWDeviceType.D3d12va)
    {
        _hwctx          = (AVD3D12VADeviceContext*)_ctx->hwctx;
        _hwctx->device  = (ID3D12Device*) device;
    }

    public D3D12VADeviceContext(string? adapterId = null, Dictionary<string, string>? opts = null) : base(AVHWDeviceType.D3d12va, adapterId, opts)
        => _hwctx = (AVD3D12VADeviceContext*)_ctx->hwctx;
}

public unsafe class OpenClDeviceContext : HWDeviceContext
{
    public int  DeviceId            => _hwctx->device_id;
    public int  Context             => _hwctx->context;
    public int  CommandQueue        => _hwctx->command_queue;
    
    public new readonly AVOpenCLDeviceContext* _hwctx;

    public OpenClDeviceContext(int deviceId) : base(AVHWDeviceType.Opencl) // caller should add ref to device?
    {
        _hwctx              = (AVOpenCLDeviceContext*)_ctx->hwctx;
        _hwctx->device_id   = deviceId;
    }

    public OpenClDeviceContext(string? adapterId = null, Dictionary<string, string>? opts = null) : base(AVHWDeviceType.Opencl, adapterId, opts)
        => _hwctx = (AVOpenCLDeviceContext*)_ctx->hwctx;
}