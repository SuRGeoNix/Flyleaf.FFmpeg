namespace Flyleaf.FFmpeg.Filter;

public unsafe abstract class BufferSinkParams { }

public unsafe class AudioBufferSinkParams : BufferSinkParams
{
    public AVSampleFormat[]?    SampleFormats       { get; set; }
    public int[]?               SampleRates         { get; set; }
    public string[]?            ChannelLayouts      { get; set; }
    public bool?                AllChannelCount     { get; set; }
}

public class VideoBufferSinkParams : BufferSinkParams
{
    public AVPixelFormat[]?     PixelFormats        { get; set; }
    public AVColorSpace[]?      ColorSpaces         { get; set; }
    public AVColorRange[]?      ColorRanges         { get; set; }
}
