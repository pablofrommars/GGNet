using GGNet.Common;
using GGNet.Data;
using GGNet.Exceptions;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;

namespace GGNet.Geoms;

internal abstract class Geom<T, TX, TY>(Source<T> source, (bool x, bool y)? scale, bool inherit) : IGeom
	where TX : struct
	where TY : struct
{
	protected readonly Source<T> source = source;
	protected readonly (bool x, bool y) scale = scale ?? (true, true);
	protected readonly bool inherit = inherit;

	private Facet<T>? facet;
	private Legends? legends;

    public Buffer<Shape> Layer { get; } = new();

    protected static IPositionMapping<T> XMapping<T1, TX1>(Func<T1, TX1> selector, Scales.Position<TX1> position)
		where TX1 : struct
	{
		if (typeof(T) != typeof(T1))
		{
			throw new GGNetUserException("Type mismatch");
		}

		if (typeof(TX1) == typeof(TX))
		{
			return new PositionMapping<T, TX1>((selector as Func<T, TX1>)!, position);
		}
		else if (typeof(TX1).IsNumeric() && typeof(TX).IsNumeric())
		{
			return new NumericalPositionMapping<T, TX1>((selector as Func<T, TX1>)!, (position as Scales.Position<double>)!);
		}
		else
		{
			throw new GGNetUserException("Type could not be infered");
		}
	}

	protected static IPositionMapping<T> XMapping<TX1>(Func<T, TX> selector, Scales.Position<TX1> position)
		where TX1 : struct
	{
		if (typeof(TX1) == typeof(TX))
		{
			return new PositionMapping<T, TX>(selector, (position as Scales.Position<TX>)!);
		}
		else if (typeof(TX1).IsNumeric() && typeof(TX).IsNumeric())
		{
			return new NumericalPositionMapping<T, TX>(selector, (position as Scales.Position<double>)!);
		}
		else
		{
			throw new GGNetUserException("Type could not be infered");
		}
	}

	protected static IPositionMapping<T> YMapping<T1, TY1>(Func<T1, TY1> selector, Scales.Position<TY1> position)
		where TY1 : struct
	{
		if (typeof(T) != typeof(T1))
		{
			throw new GGNetUserException("Type mismatch");
		}

		if (typeof(TY1) == typeof(TY))
		{
			return new PositionMapping<T, TY1>((selector as Func<T, TY1>)!, position);
		}
		else if (typeof(TY1).IsNumeric() && typeof(TX).IsNumeric())
		{
			return new NumericalPositionMapping<T, TY1>((selector as Func<T, TY1>)!, (position as Scales.Position<double>)!);
		}
		else
		{
			throw new GGNetUserException("Type could not be infered");
		}
	}

	protected static IPositionMapping<T> YMapping<TY1>(Func<T, TY> selector, Scales.Position<TY1> position)
		where TY1 : struct
	{
		if (typeof(TY1) == typeof(TY))
		{
			return new PositionMapping<T, TY>(selector, (position as Scales.Position<TY>)!);
		}
		else if (typeof(TY1).IsNumeric() && typeof(TY).IsNumeric())
		{
			return new NumericalPositionMapping<T, TY>(selector, (position as Scales.Position<double>)!);
		}
		else
		{
			throw new GGNetUserException("Type could not be infered");
		}
	}

	public virtual void Init<T1, TX1, TY1>(Panel<T1, TX1, TY1> panel, Facet<T1>? facet)
		where TX1 : struct
		where TY1 : struct
	{
		if (facet is not null && panel.Data.Source is not null && panel.Data.Source.Equals(source))
		{
			this.facet = (facet as Facet<T>)!;
		}

		legends = panel.Data.Legends;
	}

	public abstract void Train(T item);

	public void Train()
	{
		for (var i = 0; i < source.Count; i++)
		{
			var item = source[i];

			if (facet is not null && !facet.Include(item))
			{
				continue;
			}

			Train(item);
		}
	}

	protected void Legend<TV>(IAestheticMapping<T, TV>? aes, Func<TV, Elements.IElement> element)
	{
		if (legends is null)
		{
			return;
		}

		if (aes is null || !aes.Guide)
		{
			return;
		}

		var legend = legends.GetOrAdd(aes);

		var n = aes.Labels.Count();

		for (int i = 0; i < n; i++)
		{
			var (value, label) = aes.Labels.ElementAt(i);

			legend.Add(label, element(value));
		}
	}

	protected void Legend<TV>(IAestheticMapping<T, TV>? aes, Func<TV, Elements.IElement[]> elements)
	{
		if (legends is null)
		{
			return;
		}

		if (aes is null || !aes.Guide)
		{
			return;
		}

		var legend = legends.GetOrAdd(aes);

		var n = aes.Labels.Count();

		for (var i = 0; i < n; i++)
		{
			var (value, label) = aes.Labels.ElementAt(i);

			var array = elements(value);

			for (var j = 0; j < array.Length; j++)
			{
				legend.Add(label, array[j]);
			}
		}
	}

	public virtual void Legend()
	{
	}

	protected abstract void Shape(T item, bool flip);

	protected virtual void Set(bool flip)
	{
	}

	public void Shape(bool flip)
	{
		for (var i = 0; i < source.Count; i++)
		{
			var item = source[i];

			if (facet is not null && !facet.Include(item))
			{
				continue;
			}

			Shape(item, flip);
		}

		Set(flip);
	}

	public virtual void Clear() => Layer?.Clear();
}