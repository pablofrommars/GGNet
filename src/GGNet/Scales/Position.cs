using GGNet.Transformations;

using static System.Math;

namespace GGNet.Scales;

public interface IPosition : IScale
{
	void Shape(double min, double max);

	double Coord(double value);
}

public abstract class Position<TKey>(ITransformation<TKey>? transformation, (double minMult, double minAdd, double maxMult, double maxAdd) expand) : Continuous<TKey>(transformation), IPosition
	where TKey : struct
{
	protected readonly (double minMult, double minAdd, double maxMult, double maxAdd) expand = expand;

    public override void Train(TKey key) { }

	public (double min, double max) Range { get; protected set; }

	public virtual ITransformation<double> RangeTransformation { get; } = Transformations.Identity<double>.Instance;

	protected void SetRange(double min, double max)
	{
		if (min == max)
		{
			Range = (
				min - 0.05,
				max + 0.05
			);
		}
		else
		{
			if (max < min)
			{
				(max, min) = (min, max);
			}

			var range = max - min;

			Range = (
				min - (expand.minMult * range + expand.minAdd),
				max + expand.maxMult * range + expand.maxAdd
			);
		}
	}

	protected double? _min = null;
	protected double? _max = null;

	public virtual void Shape(double min, double max)
	{
		_min ??= min;
		_max ??= max;

		_min = Min(_min.Value, min);
		_max = Max(_max.Value, max);
	}

	public virtual double Coord(double value)
	{
		if (Range.min == Range.max)
		{
			return 0;
		}

		return (value - Range.min) / (Range.max - Range.min);
	}

	public override void Clear()
	{
		_min = null;
		_max = null;
	}
}

public interface IPositionMapping<T>
{
	IPosition Position { get; }

	void Train(T item);

	double Map(T item);
}

public class PositionMapping<T, TKey>(Func<T, TKey> selector, Position<TKey> position) : IPositionMapping<T>
	where TKey : struct
{
	private readonly Func<T, TKey> selector = selector;
	private readonly Position<TKey> position = position;

    public IPosition Position => position;

	public void Train(T item) => position.Train(selector(item));

	public double Map(T item) => position.Map(selector(item));
}

public class NumericalPositionMapping<T, TKey>(Func<T, TKey> selector, Position<double> scale) : IPositionMapping<T>
{
	private readonly Func<T, TKey> selector = selector;
	private readonly Position<double> scale = scale;

    public IPosition Position => scale;

	public void Train(T item) => scale.Train(Convert<TKey>.ToDouble(selector(item)));

	public double Map(T item) => scale.Map(Convert<TKey>.ToDouble(selector(item)));
}
