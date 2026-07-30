using GGNet.Transformations;

using static System.Math;

namespace GGNet.Scales;

internal interface IPosition : IScale
{
	void Shape(double min, double max);

	double Coord(double value);

	double Invert(double coord);
}

internal abstract class Position<TKey>(ITransformation<TKey>? transformation, (double minMult, double minAdd, double maxMult, double maxAdd) expand) : Continuous<TKey>(transformation), IPosition
	where TKey : struct
{
	protected readonly (double minMult, double minAdd, double maxMult, double maxAdd) expand = expand;

	public override void Train(TKey key) { }

	public (double min, double max) Range { get; protected set; }

	public virtual ITransformation<double> RangeTransformation { get; } = Transformations.Identity<double>.Instance;

	// The one place data space becomes scale space for an endpoint. Explicit Limits are
	// authored in data units, while _min/_max arrive from the geoms already mapped, so a
	// limit on a transformed scale has to go through Map before it can meet SetRange.
	protected double Endpoint(TKey? limit, double? trained) => limit.HasValue ? Map(limit.Value) : trained ?? 0.0;

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

	// Realization bucket: data-trained bounds, re-trained every pass.
	protected double? _min;
	protected double? _max;

	// Interaction bucket: the runtime view window, in transformed double space,
	// written by host commands and pixel gestures. Survives Clear() — only its
	// owner (reset-view) clears it — and bypasses SetRange's expansion: a zoom
	// shows the exact window, unpadded.
	public (double min, double max)? ViewRange { get; set; }

	// True when the view window took the range; Commit then derives breaks and
	// labels from the windowed Range like any other pass.
	protected bool CommitViewRange()
	{
		if (ViewRange is not { } view)
		{
			return false;
		}

		Range = view.max < view.min ? (view.max, view.min) : view;

		return true;
	}

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

	// Inverse of Coord: a fraction of the committed range back into the
	// scale's double space. A degenerate range collapses to Range.min,
	// mirroring Coord's zero.
	public double Invert(double coord) => Range.min + coord * (Range.max - Range.min);

	// Typed inverse of Map, for the coordinate readout: continuous scales
	// invert the transformation, discrete scales snap to the nearest trained
	// value; null when the mapped value has no meaningful key.
	public virtual TKey? Unmap(double value) => null;

	// Readout text for a data value, through the scale's own formatter —
	// invariant unless the author opted into a localized formatter.
	public virtual string? Label(TKey value) => null;

	// Clears realization only: spec (Limits) and durable interaction state
	// (the runtime view window, when it lands) must survive the per-pass Reset().
	public override void Clear()
	{
		_min = null;
		_max = null;
	}
}

internal interface IPositionMapping<T>
{
	IPosition Position { get; }

	void Train(T item);

	double Map(T item);
}

internal class PositionMapping<T, TKey>(Func<T, TKey> selector, Position<TKey> position) : IPositionMapping<T>
	where TKey : struct
{
	private readonly Func<T, TKey> selector = selector;
	private readonly Position<TKey> position = position;

	public IPosition Position => position;

	public void Train(T item) => position.Train(selector(item));

	public double Map(T item) => position.Map(selector(item));
}

