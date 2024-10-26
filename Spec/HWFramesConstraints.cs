namespace Flyleaf.FFmpeg.Spec;

public unsafe class HWFramesConstraints
{
    //static Dictionary<AVHWDeviceType, HWFramesConstraints> fcs = [];

    public int      MinWidth        { get; private set; }
    public int      MinHeight       { get; private set; }
    public int      MaxWidth        { get; private set; }
    public int      MaxHeight       { get; private set; }
    public List<AVPixelFormat>
                    HWFormats       { get; private set; } = [];
    public List<AVPixelFormat>
                    SWFormats       { get; private set; } = [];

    private HWFramesConstraints(HWDeviceContextBase device)
    {
        var fcs     = av_hwdevice_get_hwframe_constraints(device._ptr, null); // hwconfig probably unused
        if (fcs == null)
        {
            FillMissing(device.Type);
            return;
        }

        MinWidth    = fcs->min_width;
        MinHeight   = fcs->min_height;
        MaxWidth    = fcs->max_width;
        MaxHeight   = fcs->max_height;

        int i = 0;
        while (fcs->valid_hw_formats[i] != AVPixelFormat.None)
            HWFormats.Add(fcs->valid_hw_formats[i++]);

        i = 0;
        while (fcs->valid_sw_formats[i] != AVPixelFormat.None)
            SWFormats.Add(fcs->valid_sw_formats[i++]);

        av_hwframe_constraints_free(ref fcs);

        //HWFramesConstraints.fcs[device.Type] = this;
    }

    void FillMissing(AVHWDeviceType type) // NOTE: av_hwdevice_get_hwframe_constraints supposely check for supported for the specific device/adapter while here we just return all supported***
    {
        MaxWidth = MaxHeight = int.MaxValue;
        if (type == AVHWDeviceType.Dxva2)
        {
            HWFormats.Add(AVPixelFormat.Dxva2Vld);
            SWFormats = [AVPixelFormat.Nv12, AV_PIX_FMT_P010, AVPixelFormat.Vuyx, AVPixelFormat.Yuyv422, AV_PIX_FMT_Y210, AV_PIX_FMT_XV30, AV_PIX_FMT_P012, AV_PIX_FMT_Y212, AV_PIX_FMT_XV36, AVPixelFormat.Pal8, AVPixelFormat.Bgra];
        }
    }

    public static HWFramesConstraints? Get(HWDeviceContextBase device)
    {
        return new(device);
        // TBR: if we pass a custom device or a different adapterId then we will not have the same results*?
        // lock(fcs) return fcs.TryGetValue(device.Type, out var fcsCache) ? fcsCache : new(device);
    }
}
