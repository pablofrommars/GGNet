using GGNet.Common;

namespace GGNet.Scales;

public class Identity<T> : Scale<T, T>
{
	public Identity() : base(null) { }

	public override Guide Guide => Guide.Items;

	public override void Train(T key) { }

	public override void Set(bool grid) { }

	public override T Map(T key) => key;

	public override void Clear() { }
}