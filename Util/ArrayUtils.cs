namespace Flyleaf.FFmpeg;

public unsafe static class ArrayUtils
{
    public static List<AVChannelLayout> GetChannelLayouts(AVChannelLayout* chLayouts)
    {
        List<AVChannelLayout> list = [];

        if (chLayouts != null)
        {
            int i = 0;
            while (chLayouts[i].nb_channels != 0) // TBR: terminated with a zeroed layout?
                list.Add(chLayouts[i++]);
        }

        return list;
    }

    public static List<AVCodecID> GetCodecIds(AVCodecID* codecIds)
    {
        List<AVCodecID> list = [];

        if (codecIds != null)
        {
            int i = 0;
            while (codecIds[i] != AVCodecID.None)
                list.Add(codecIds[i++]);
        }

        return list;
    }

    public static List<AVRational> GetFrameRates(AVRational* frameRates)
    {
        List<AVRational> list = [];

        if (frameRates != null)
        {
            int i = 0;
            while (frameRates[i].Num != 0 || frameRates[i].Den != 0)
                list.Add(frameRates[i++]);
        }

        return list;
    }

    public static List<AVCodecHWConfig> GetHWConfigs(AVCodec* codec)
    {
        List<AVCodecHWConfig> list = [];

        int i = 0;
        AVCodecHWConfig* config;
        while((config = avcodec_get_hw_config(codec, i++)) != null)
            list.Add(*config);

        return list;
    }

    public static List<AVCodecHWConfig> GetHWDecoderConfigs(AVCodec* codec)
    {
        List<AVCodecHWConfig> list = [];

        int i = 0;
        AVCodecHWConfig* config;
        while((config = avcodec_get_hw_config(codec, i++)) != null)
            if (config->device_type != AVHWDeviceType.None) // Excluded ADHOC (D3D11VAVLD) seems deprecated? -> https://ffmpeg.org/doxygen/trunk/codec_8h_source.html#l00323
            list.Add(*config); // currently all decoders have those -> config->methods.HasFlag(CodecHWConfigMethodFlags.HWDeviceCtx) && config->methods.HasFlag(CodecHWConfigMethodFlags.HWFramesCtx)

        return list;
    }

    public static List<string> GetMimeTypes(byte** ptr)
    {
        List<string> ret = [];
        
        if (ptr != null)
        {
            int i = 0;
            while (ptr[i] != null)
                ret.Add(GetString(ptr[i++])!);            
        }

        return ret;
    }

    public static List<AVPixelFormat> GetPixelFormats(AVPixelFormat* pixelFormats)
    {
        List<AVPixelFormat> list = [];

        if (pixelFormats != null)
        {
            int i = 0;
            while (pixelFormats[i] != AVPixelFormat.None)
                list.Add(pixelFormats[i++]);
        }

        return list;
    }

    public static List<CodecProfile> GetProfiles(AVProfile* profile)
    {
        List<CodecProfile> list = [];

        if (profile != null)
        {
            int i = 0;
            while (profile[i].profile != AV_PROFILE_UNKNOWN)
                list.Add(GetProfile(&profile[i++]));
        }

        return list;
    }

    public static List<int> GetSampleRates(int* sampleRates)
    {
        List<int> list = [];

        if (sampleRates != null)
        {
            int i = 0;
            while (sampleRates[i] != 0)
                list.Add(sampleRates[i++]);
        }

        return list;
    }

    public static List<AVCodecTag> GetTags(AVCodecTag** tags)
    {
        List<AVCodecTag> list = [];
        
        if (tags != null)
        {
            nint cur = (nint)(*tags);
            while (((AVCodecTag*)cur)->id != AVCodecID.None)
            {
                list.Add(*(AVCodecTag*)cur);
                cur += sizeof(AVCodecTag);
            }
        }

        return list;
    }

    public static List<AVSampleFormat> GetSampleFormats(AVSampleFormat* sampleFormats)
    {
        List<AVSampleFormat> list = [];

        if (sampleFormats != null)
        {
            int i = 0;
            while (sampleFormats[i] != AVSampleFormat.None)
                list.Add(sampleFormats[i++]);
        }

        return list;
    }
}
