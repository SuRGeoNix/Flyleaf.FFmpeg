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
        if (type == AVHWDeviceType.DXVA2)
        {
            HWFormats.Add(AVPixelFormat.DXVA2Vld);
            SWFormats = [AVPixelFormat.NV12, AVPixelFormat.P010le, AVPixelFormat.Vuyx, AVPixelFormat.Yuyv422, AVPixelFormat.Y210le, AVPixelFormat.Xv30le, AVPixelFormat.P012le, AVPixelFormat.Y212le, AVPixelFormat.Xv36le, AVPixelFormat.Pal8, AVPixelFormat.BGRA];
        }
    }

    public static HWFramesConstraints? Get(HWDeviceContextBase device)
    {
        return new(device);
        // TBR: if we pass a custom device or a different adapterId then we will not have the same results*?
        // lock(fcs) return fcs.TryGetValue(device.Type, out var fcsCache) ? fcsCache : new(device);
    }
}
