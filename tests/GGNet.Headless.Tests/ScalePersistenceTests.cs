using GGNet.Scales;

namespace GGNet.Headless.Tests;

// Phase 3 — the persist/clear seam interactivity's runtime view window will rely
// on (§8c #1): Clear() drops the data-trained bounds but must NOT drop Limits,
// and Commit honors Limits over the trained data. Position<double> behaviour is
// exercised through Extended; DiscretePosition adds its values-buffer reset.
public class ScalePersistenceTests
{
	[Fact]
	public void ExtendedCommitHonorsLimitsOverTrainedBounds()
	{
		// Arrange

		var sut = new Extended(limits: (5.0, 10.0));
		sut.Shape(0.0, 100.0);

		// Act

		sut.Commit(grid: false);

		// Assert

		// Range derives from Limits (5, 10) with the 5% expansion, not the
		// trained (0, 100).
		using var _ = new AssertionScope();

		sut.Range.min.Should().BeApproximately(4.75, 1e-9);
		sut.Range.max.Should().BeApproximately(10.25, 1e-9);
	}

	[Fact]
	public void ClearDropsTrainedBounds()
	{
		// Arrange

		var sut = new Extended();
		sut.Shape(0.0, 100.0);

		// Act

		sut.Clear();
		sut.Commit(grid: false);

		// Assert

		// With trained bounds gone and no Limits, SetRange(0, 0) collapses to ±0.05.
		using var _ = new AssertionScope();

		sut.Range.min.Should().BeApproximately(-0.05, 1e-9);
		sut.Range.max.Should().BeApproximately(0.05, 1e-9);
	}

	[Fact]
	public void ClearPreservesLimits()
	{
		// Arrange

		var sut = new Extended(limits: (5.0, 10.0));
		sut.Shape(0.0, 100.0);

		// Act

		sut.Clear();
		sut.Commit(grid: false);

		// Assert

		// Clear dropped the trained bounds, but Limits survived and still drive
		// the range — the exact invariant a runtime ViewRange depends on.
		using var _ = new AssertionScope();

		sut.Range.min.Should().BeApproximately(4.75, 1e-9);
		sut.Range.max.Should().BeApproximately(10.25, 1e-9);
	}

	[Fact]
	public void DiscretePositionClearEmptiesValues()
	{
		// Arrange

		var sut = new DiscretePosition<int>();
		sut.Train(1);
		sut.Train(2);

		// Act / Assert

		using var _ = new AssertionScope();

		sut.Map(1).Should().Be(0.0);
		sut.Map(2).Should().Be(1.0);

		sut.Clear();

		double.IsNaN(sut.Map(1)).Should().BeTrue();
		double.IsNaN(sut.Map(2)).Should().BeTrue();
	}
}
