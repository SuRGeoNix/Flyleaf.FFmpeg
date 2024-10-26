namespace Flyleaf.FFmpeg;

public readonly struct FFmpegResult(int result)
{
    public static readonly FFmpegResult Default = new(0);

    public readonly int     Result          = result;

    public readonly bool    Success         => Result >= 0;
    public readonly bool    Failed          => Result <  0;

    public readonly bool    Eof             => Result == AVERROR_EOF;
    public readonly bool    ImmediateExit   => Result == AVERROR_EXIT;
    public readonly bool    InvalidData     => Result == AVERROR_INVALIDDATA;
    public readonly bool    InvalidArgument => Result == AVERROR_EINVAL;
    public readonly bool    NoMemory        => Result == AVERROR_ENOMEM;
    public readonly bool    TryAgain        => Result == AVERROR_EAGAIN;

    public void ThrowOnFailure() { if (Result < 0) throw new FFmpegException(Result); }
    public readonly override string ToString() => Success ? "Success" : FFmpegException.GetErrorMsg(Result);
}

public class FFmpegException(int errorCode) : Exception()
{
    public int              ErrorCode   { get; } = errorCode;
    public override string  Message     { get; } = GetErrorMsg(errorCode);

    const string NonFFmpegError = "A unknown non-FFmpeg error occurred ({0})";
    public unsafe static string GetErrorMsg(int errorCode)
    {
        byte* buffer = stackalloc byte[AV_ERROR_MAX_STRING_SIZE];
        string? res  = av_strerror(errorCode, buffer, AV_ERROR_MAX_STRING_SIZE) < 0 ? null : GetString(buffer);
        return res ?? string.Format(NonFFmpegError, errorCode);
    }
}
