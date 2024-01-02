namespace GGNet.Theme;

public sealed class Strip(bool dark)
{
    public StripText Text { get; set; } = new(dark);
}