using System.Runtime.CompilerServices;

using GGNet.Exceptions;

namespace GGNet.Readings;

internal static class InspectAxis
{
	public static bool Supports<TX>()
		where TX : struct
		=> typeof(TX) == typeof(Instant) || typeof(TX) == typeof(double);

	public static double Coordinate<TX>(TX value)
		where TX : struct
	{
		if (typeof(TX) == typeof(Instant))
		{
			return Unsafe.As<TX, Instant>(ref value).ToUnixTimeTicks() / (double)NodaConstants.TicksPerMillisecond;
		}

		if (typeof(TX) == typeof(double))
		{
			return Unsafe.As<TX, double>(ref value);
		}

		throw Unsupported<TX>();
	}

	public static TX Value<TX>(double coordinate)
		where TX : struct
	{
		if (typeof(TX) == typeof(Instant))
		{
			var instant = Instant.FromUnixTimeTicks((long)Math.Round(coordinate * NodaConstants.TicksPerMillisecond));

			return Unsafe.As<Instant, TX>(ref instant);
		}

		if (typeof(TX) == typeof(double))
		{
			return Unsafe.As<double, TX>(ref coordinate);
		}

		throw Unsupported<TX>();
	}

	public static Func<T, double> Coordinate<T, TX>(Func<T, TX> selector)
		where TX : struct
		=> item => Coordinate(selector(item));

	public static Func<T, bool> HasValue<T, TY>(Func<T, TY> selector)
		where TY : struct
	{
		if (typeof(TY) != typeof(double))
		{
			return _ => true;
		}

		return item =>
		{
			var value = selector(item);

			return !double.IsNaN(Unsafe.As<TY, double>(ref value));
		};
	}

	public static double? Limit<T, TX>(Inspect<T> inspect)
		where TX : struct
	{
		if (typeof(TX) == typeof(Instant))
		{
			if (inspect.MaxDistance is not null)
			{
				throw new GGNetUserException($"inspect \"{inspect.Name}\": the x axis is Instant; give the limit as MaxDuration, not MaxDistance");
			}

			return NonNegative(inspect, inspect.MaxDuration?.TotalMilliseconds);
		}

		if (typeof(TX) == typeof(double))
		{
			if (inspect.MaxDuration is not null)
			{
				throw new GGNetUserException($"inspect \"{inspect.Name}\": the x axis is numeric; give the limit as MaxDistance, not MaxDuration");
			}

			return NonNegative(inspect, inspect.MaxDistance);
		}

		throw Unsupported<TX>();
	}

	public static Reading<TX> Reading<TX>(SeriesReading reading, double cursor)
		where TX : struct
		=> new(reading.Series, reading.Group, reading.Name, Value<TX>(cursor), reading.Sample is { } sample ? Value<TX>(sample) : null, reading.Kind, reading.Value);

	public static GGNetUserException Unsupported<TX>()
		=> new($"inspect: readings need an Instant or numeric x axis; this plot's x is {typeof(TX).Name}");

	private static double? NonNegative<T>(Inspect<T> inspect, double? limit)
		=> limit is < 0 or double.NaN
			? throw new GGNetUserException($"inspect \"{inspect.Name}\": the maximum distance cannot be negative")
			: limit;
}
