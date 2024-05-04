namespace GGNet;

public enum RenderTarget : ushort
{
	None = 0,
	Loading = 1,
	Data = 1 << 2,
	Theme = 1 << 4,
	All = 0xffff
}
