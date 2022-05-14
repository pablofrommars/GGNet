namespace GGNet.Theme;

public sealed class Strip
{
	public Strip(bool dark)
	{
		Text = new(dark);
	}

	public StripText Text { get; set; }
}