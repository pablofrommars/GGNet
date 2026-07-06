using GGNet.Components;

using DiscretePosition = GGNet.Scales.DiscretePosition<double>;

namespace GGNet.Headless.Tests;

// The grid memo skips layout and grid composition when the stamp matches;
// a false positive would freeze the grid, so equality must be structural
// over every composition input.
public class GridStampTests
{
	private static readonly Zone zone = new() { X = 10, Y = 20, Width = 300, Height = 200 };

	private static DiscretePosition Scale(params double[] keys)
	{
		var scale = new DiscretePosition();

		foreach (var key in keys)
		{
			scale.Train(key);
		}

		scale.Shape(0, keys.Length - 1);
		scale.Commit(true);

		return scale;
	}

	[Fact]
	public void IdenticalStateMatches()
	{
		// Arrange

		var x = Scale(1, 2, 3);
		var y = Scale(10, 20);

		// Act

		var first = GridStamp.Capture(zone, x, y, (30, 12));
		var second = GridStamp.Capture(zone, x, y, (30, 12));

		// Assert

		second.Matches(first).Should().BeTrue();
	}

	[Fact]
	public void NullNeverMatches()
	{
		// Arrange

		var stamp = GridStamp.Capture(zone, Scale(1), Scale(1), (0, 0));

		// Act

		// Assert

		stamp.Matches(null).Should().BeFalse();
	}

	[Fact]
	public void RetrainedScaleBreaksTheMatch()
	{
		// Arrange

		var x = Scale(1, 2, 3);
		var y = Scale(10, 20);

		var before = GridStamp.Capture(zone, x, y, (30, 12));

		// Act

		// A new key arrives: range, breaks and labels all move.
		x.Train(4);
		x.Shape(0, 3);
		x.Commit(true);

		var after = GridStamp.Capture(zone, x, y, (30, 12));

		// Assert

		after.Matches(before).Should().BeFalse();
	}

	[Fact]
	public void ResizedZoneBreaksTheMatch()
	{
		// Arrange

		var x = Scale(1, 2);
		var y = Scale(1, 2);

		var before = GridStamp.Capture(zone, x, y, (0, 0));

		// Act

		var resized = zone;
		resized.Width += 1;

		var after = GridStamp.Capture(resized, x, y, (0, 0));

		// Assert

		after.Matches(before).Should().BeFalse();
	}

	[Fact]
	public void AxisAggregateBreaksTheMatch()
	{
		// Arrange

		var x = Scale(1, 2);
		var y = Scale(1, 2);

		var before = GridStamp.Capture(zone, x, y, (30, 12));

		// Act

		// A longer label elsewhere widened the shared axis band.
		var after = GridStamp.Capture(zone, x, y, (34, 12));

		// Assert

		after.Matches(before).Should().BeFalse();
	}
}
