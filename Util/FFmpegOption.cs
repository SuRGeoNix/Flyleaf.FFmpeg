namespace Flyleaf.FFmpeg;

public unsafe class FFmpegOption
{
    public string       Name            => GetString(opt->name)!;
    public string?      Help            => GetString(opt->help);
    public int          Offset          => opt->offset;
    public AVOptionType Type            => opt->type;
    public object?      DefaultValue    => Type switch
    {
        AVOptionType.String     => GetString(opt->default_val.str),
        AVOptionType.Rational   => opt->default_val.q,
        AVOptionType.Float      => opt->default_val.dbl,
        _                       => (IntPtr)opt->default_val.str
    };
    public double       Min             => opt->min;
    public double       Max             => opt->max;
    public OptFlags     Flags           => opt->flags;
    public string?      Unit            => GetString(opt->unit);

    private readonly AVOption* opt;
    public static implicit operator AVOption*(FFmpegOption data)
        => data.opt;

    public FFmpegOption(nint option) : this((AVOption*)option) { }

    public FFmpegOption(AVOption* option)
    {
        ArgumentNullException.ThrowIfNull(option);
        opt = option;
    }
}


