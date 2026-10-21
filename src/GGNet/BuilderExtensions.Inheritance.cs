using GGNet.Exceptions;
using GGNet.Scales;

namespace GGNet;

public static partial class BuilderExtensions
{
	private static IAestheticMapping<TLayer, TValue>? ResolveMapping<TPlot, TLayer, TValue>(
		IAestheticMapping<TLayer, TValue>? explicitMapping,
		IAestheticMapping<TPlot, TValue>? inheritedMapping,
		bool inherit,
		string parameter)
	{
		if (explicitMapping is not null || !inherit || inheritedMapping is null)
		{
			return explicitMapping;
		}

		if (inheritedMapping is IAestheticMapping<TLayer, TValue> compatibleMapping)
		{
			return compatibleMapping;
		}

		throw new GGNetUserException($"Cannot inherit {parameter}: the plot mapping for row type '{typeof(TPlot)}' is incompatible with layer row type '{typeof(TLayer)}'. Supply an explicit {parameter} mapping for the layer or set inherit: false.");
	}
}
