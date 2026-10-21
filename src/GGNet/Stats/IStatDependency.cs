namespace GGNet;

internal interface IStatDependency
{
	IStatSource? Upstream { get; }
}
