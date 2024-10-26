namespace Flyleaf.FFmpeg.Spec;

public static class HWDeviceSpec
{
    public static AVHWDeviceType FindHWDevice(AVPixelFormat pixelFormat) => HWDeviceFromPixelFormats.TryGetValue(pixelFormat, out var deviceType) ? deviceType : AVHWDeviceType.None;
    public static AVPixelFormat FindHWPixelFormat(AVHWDeviceType deviceType) => HWDeviceByPixelFormats.TryGetValue(deviceType , out var pixelFormat) ? pixelFormat : AVPixelFormat.None;

    public static readonly List<AVHWDeviceType> HWDevices = [];
    public static readonly Dictionary<AVHWDeviceType, AVPixelFormat> HWDeviceByPixelFormats = [];
    public static readonly Dictionary<AVPixelFormat, AVHWDeviceType> HWDeviceFromPixelFormats = [];

    static Dictionary<AVHWDeviceType, AVPixelFormat> _HWDeviceByPixelFormats = new()
    {
        [AVHWDeviceType.Vdpau]          = AVPixelFormat.Vdpau,
        [AVHWDeviceType.Cuda]           = AVPixelFormat.Cuda,
        [AVHWDeviceType.Vaapi]          = AVPixelFormat.Vaapi,
        [AVHWDeviceType.Dxva2]          = AVPixelFormat.Dxva2Vld,
        [AVHWDeviceType.Qsv]            = AVPixelFormat.Qsv,
        [AVHWDeviceType.Videotoolbox]   = AVPixelFormat.Videotoolbox,
        [AVHWDeviceType.D3d11va]        = AVPixelFormat.D3d11,
        [AVHWDeviceType.Drm]            = AVPixelFormat.DrmPrime,
        [AVHWDeviceType.Opencl]         = AVPixelFormat.Opencl,
        [AVHWDeviceType.Mediacodec]     = AVPixelFormat.Mediacodec,
        [AVHWDeviceType.Vulkan]         = AVPixelFormat.Vulkan,
        [AVHWDeviceType.D3d12va]        = AVPixelFormat.D3d12,
    };

    //static Dictionary<AVPixelFormat, AVHWDeviceType> _HWDeviceFromPixelFormats = new()
    //{
    //    [AVPixelFormat.Vdpau]           = AVHWDeviceType.Vdpau,
    //    [AVPixelFormat.Cuda]            = AVHWDeviceType.Cuda,
    //    [AVPixelFormat.Vaapi]           = AVHWDeviceType.Vaapi,
    //    [AVPixelFormat.Dxva2Vld]        = AVHWDeviceType.Dxva2,
    //    [AVPixelFormat.Qsv]             = AVHWDeviceType.Qsv,
    //    [AVPixelFormat.Videotoolbox]    = AVHWDeviceType.Videotoolbox,
    //    [AVPixelFormat.D3d11]           = AVHWDeviceType.D3d11va,
    //    [AVPixelFormat.DrmPrime]        = AVHWDeviceType.Drm,
    //    [AVPixelFormat.Opencl]          = AVHWDeviceType.Opencl,
    //    [AVPixelFormat.Mediacodec]      = AVHWDeviceType.Mediacodec,
    //    [AVPixelFormat.Vulkan]          = AVHWDeviceType.Vulkan,
    //    [AVPixelFormat.D3d12]           = AVHWDeviceType.D3d12va,
    //};

    public static void FillHWDevices()
    {
        AVHWDeviceType cur = AVHWDeviceType.None;
        while ((cur = av_hwdevice_iterate_types(cur)) != AVHWDeviceType.None)
        {
            HWDevices.Add(cur);
            HWDeviceByPixelFormats[cur] = _HWDeviceByPixelFormats[cur];
            HWDeviceFromPixelFormats[_HWDeviceByPixelFormats[cur]] = cur;
        }
    }
}
