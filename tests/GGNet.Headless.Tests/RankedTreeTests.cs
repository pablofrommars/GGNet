using System.Collections;

namespace GGNet.Headless.Tests;

public class RankedTreeTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(15)]
	[InlineData(16)]
	[InlineData(17)]
	[InlineData(255)]
	[InlineData(256)]
	[InlineData(257)]
	public void InteriorInsertionPromotesOnceAndKeepsAppendOnlyNodeStorage(int length)
	{
		// Arrange

		var tree = new RankedTree<int>();
		for (var i = 0; i < length; i++)
		{
			tree.Add(i * 2, duplicates: false);
		}

		var prefix = Field(tree, "prefix");
		var before = Nodes(tree).Count;

		// Act

		tree.Add(-1, duplicates: false);
		var storage = Nodes(tree);
		var initialItems = storage.Cast<object>().Select(node => (int)Field(node, "Item")!).ToArray();
		var checks = new List<bool> { Valid(tree) };
		for (var i = 0; i < length; i++)
		{
			tree.Add(i * 2 + 1, duplicates: false);
			checks.Add(Valid(tree));
		}

		var finalItems = storage.Cast<object>().Select(node => (int)Field(node, "Item")!).ToArray();

		// Assert

		using var _ = new AssertionScope();
		prefix.Should().NotBeNull();
		before.Should().Be(0);
		Field(tree, "prefix").Should().BeNull();
		Nodes(tree).Should().BeSameAs(storage);
		initialItems.Should().Equal(Enumerable.Range(0, length).Select(i => i * 2).Append(-1));
		finalItems.Should().Equal(initialItems.Concat(Enumerable.Range(0, length).Select(i => i * 2 + 1)));
		checks.Should().OnlyContain(valid => valid);
		Enumerable.Range(0, tree.Count).Select(i => tree[i]).Should().Equal(Enumerable.Range(-1, length * 2 + 1));
	}

	[Fact]
	public void AscendingInputKeepsPayloadOnlyStorageAndLinearComparerWork()
	{
		// Arrange

		var comparisons = 0;
		var comparer = Comparer<int>.Create((a, b) => { comparisons++; return a.CompareTo(b); });
		var tree = new RankedTree<int>(comparer);
		var observed = new List<int>();

		// Act

		for (var i = 0; i < 4096; i++)
		{
			tree.Add(i, duplicates: false);
			observed.Add(tree.Count + tree[i]);
		}

		// Assert

		using var _ = new AssertionScope();
		comparisons.Should().Be(4095);
		observed.Should().Equal(Enumerable.Range(0, 4096).Select(i => i * 2 + 1));
		Field(tree, "prefix").Should().NotBeNull();
		Nodes(tree).Count.Should().Be(0);
		Capacity(tree).Should().Be(0);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void AdversarialInsertionHasBoundedComparisonsAndBalancedStorage(bool samples)
	{
		// Arrange

		var comparisons = 0;
		var comparer = Comparer<int>.Create((a, b) => { comparisons++; return a.CompareTo(b); });
		var tree = new RankedTree<int>(comparer);
		var observed = new List<int>();
		var values = Enumerable.Range(0, 4096).Reverse().Select(i => samples ? i % 7 : i).ToArray();

		// Act

		foreach (var value in values)
		{
			tree.Add(value, samples);
			observed.Add(tree[tree.Count / 2]);
		}

		// Assert

		using var _ = new AssertionScope();
		comparisons.Should().BeLessThan(8 * values.Length * 13);
		observed.Should().HaveCount(values.Length);
		Valid(tree).Should().BeTrue();
		Field(tree, "prefix").Should().BeNull();
		Nodes(tree).Count.Should().Be(values.Length);
		Enumerable.Range(0, tree.Count).Select(i => tree[i]).Should().Equal(values.Order());
	}

	[Fact]
	public void UniqueDuplicatesAndPayloadWritesDoNotPromote()
	{
		// Arrange

		var comparer = Comparer<(int Key, int Payload)>.Create((a, b) => a.Key.CompareTo(b.Key));
		var tree = new RankedTree<(int Key, int Payload)>(comparer);
		tree.Add((1, 10), duplicates: false);
		tree.Add((3, 30), duplicates: false);

		// Act

		tree[0] = (1, 11);
		tree.Add((1, 90), duplicates: false);
		tree.Add((3, 99), duplicates: false);
		var prefixItems = Enumerable.Range(0, tree.Count).Select(i => tree[i]).ToArray();
		var prefixMode = Field(tree, "prefix");
		tree.Add((2, 20), duplicates: false);
		tree[1] = (2, 22);

		// Assert

		using var _ = new AssertionScope();
		prefixMode.Should().NotBeNull();
		prefixItems.Should().Equal((1, 11), (3, 30));
		Enumerable.Range(0, tree.Count).Select(i => tree[i]).Should().Equal((1, 11), (2, 22), (3, 30));
		tree.IndexOf((2, 0)).Should().Be(1);
	}

	[Fact]
	public void ClearRetainsTreeCapacityForOnlyOneIdleEpoch()
	{
		// Arrange

		var tree = new RankedTree<int>();
		foreach (var value in Enumerable.Range(0, 256).Reverse())
		{
			tree.Add(value, duplicates: false);
		}

		var originalCapacity = Capacity(tree);

		// Act

		tree.Clear();
		var retained = Capacity(tree);
		var clearedCount = Nodes(tree).Count;
		tree.Add(2, duplicates: false);
		tree.Add(1, duplicates: false);
		var reused = Capacity(tree);
		tree.Clear();
		tree.Add(1, duplicates: false);
		tree.Add(2, duplicates: false);
		tree.Clear();
		var released = Capacity(tree);
		tree.Add(7, duplicates: false);

		// Assert

		using var _ = new AssertionScope();
		retained.Should().Be(originalCapacity);
		clearedCount.Should().Be(0);
		reused.Should().Be(originalCapacity);
		released.Should().Be(0);
		Field(tree, "prefix").Should().NotBeNull();
		tree.Count.Should().Be(1);
		tree[0].Should().Be(7);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void ComparisonFailuresPreserveBothModesWithoutDoubleWrapping(bool promoted, bool tailFailure)
	{
		// Arrange

		var failure = false;
		var comparer = Comparer<int>.Create((a, b) => failure && (tailFailure || a != 6) ? throw new ArgumentException("Comparison failed") : a.CompareTo(b));
		var tree = new RankedTree<int>(comparer);
		tree.Add(2, duplicates: false);
		tree.Add(6, duplicates: false);
		if (promoted)
		{
			tree.Add(4, duplicates: false);
		}

		var before = Enumerable.Range(0, tree.Count).Select(i => tree[i]).ToArray();
		var prefix = Field(tree, "prefix");
		failure = true;

		// Act

		var exception = Record.Exception(() => tree.Add(0, duplicates: false));

		// Assert

		using var _ = new AssertionScope();
		exception.Should().BeOfType<InvalidOperationException>().Which.InnerException.Should().BeOfType<ArgumentException>();
		Field(tree, "prefix").Should().BeSameAs(prefix);
		Enumerable.Range(0, tree.Count).Select(i => tree[i]).Should().Equal(before);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(16)]
	[InlineData(256)]
	public void EqualSamplePromotesAtTheLegacyMidpointRank(int length)
	{
		// Arrange

		var comparer = Comparer<(int Key, int Payload)>.Create((a, b) => a.Key.CompareTo(b.Key));
		var tree = new RankedTree<(int Key, int Payload)>(comparer);
		var expected = new List<(int Key, int Payload)>();
		for (var i = 0; i < length; i++)
		{
			tree.Add((i, i), duplicates: true);
			expected.Add((i, i));
		}

		var modeBefore = Field(tree, "prefix");
		var item = (length / 2, -1);
		var rank = expected.BinarySearch(item, comparer);
		expected.Insert(rank, item);

		// Act

		tree.Add(item, duplicates: true);

		// Assert

		using var _ = new AssertionScope();
		modeBefore.Should().NotBeNull();
		Field(tree, "prefix").Should().BeNull();
		Enumerable.Range(0, tree.Count).Select(i => tree[i]).Should().Equal(expected);
		Valid(tree).Should().BeTrue();
	}

	private static object? Field(object target, string name)
		=> target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(target);

	private static IList Nodes(object tree) => (IList)Field(tree, "nodes")!;

	private static int Capacity(object tree) => (int)Nodes(tree).GetType().GetProperty("Capacity")!.GetValue(Nodes(tree))!;

	private static bool Valid(object tree)
	{
		var nodes = Nodes(tree);
		var ordered = new List<int>();
		var visited = new HashSet<int>();
		var valid = true;
		(int Height, int Size) Walk(int index)
		{
			if (index < 0)
			{
				return (0, 0);
			}

			if (index >= nodes.Count || !visited.Add(index))
			{
				valid = false;
				return (0, 0);
			}

			var node = nodes[index]!;
			var left = Walk((int)Field(node, "Left")!);
			ordered.Add(index);
			var right = Walk((int)Field(node, "Right")!);
			var height = Math.Max(left.Height, right.Height) + 1;
			var size = left.Size + right.Size + 1;
			valid &= Math.Abs(left.Height - right.Height) <= 1 && (int)Field(node, "Height")! == height && (int)Field(node, "Size")! == size;
			return (height, size);
		}

		Walk((int)Field(tree, "root")!);
		valid &= visited.Count == nodes.Count;
		for (var i = 0; i < ordered.Count; i++)
		{
			var node = nodes[ordered[i]]!;
			valid &= (int)Field(node, "Previous")! == (i == 0 ? -1 : ordered[i - 1]);
			valid &= (int)Field(node, "Next")! == (i == ordered.Count - 1 ? -1 : ordered[i + 1]);
		}

		return valid && (int)Field(tree, "first")! == (ordered.Count == 0 ? -1 : ordered[0]) && (int)Field(tree, "last")! == (ordered.Count == 0 ? -1 : ordered[^1]);
	}
}
