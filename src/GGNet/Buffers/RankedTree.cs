using System.Runtime.InteropServices;

namespace GGNet.Buffers;

internal sealed partial class RankedTree<T>(IComparer<T>? comparer = null)
{
	private struct Node(T item)
	{
		public T Item = item;
		public int Left = -1;
		public int Right = -1;
		public int Height = 1;
		public int Size = 1;
		public int Previous = -1;
		public int Next = -1;
	}

	private readonly IComparer<T> comparer = comparer ?? Comparer<T>.Default;
	private readonly List<Node> nodes = [];
	private List<T>? prefix = [];
	private int root = -1;
	private int first = -1;
	private int last = -1;
	private long cursor = -1;

	public int Count => prefix?.Count ?? nodes.Count;

	public T this[int index]
	{
		get
		{
			if (prefix is not null)
			{
				Visit();
				return prefix[index];
			}

			return Entry(FindNode(index)).Item;
		}
		set
		{
			if (prefix is not null)
			{
				Visit();
				prefix[index] = value;
			}
			else
			{
				Entry(FindNode(index)).Item = value;
			}
		}
	}

	private ref Node Entry(int index)
	{
		Visit();
		return ref CollectionsMarshal.AsSpan(nodes)[index];
	}

	partial void Visit();

	partial void Grow(int count);

	partial void Promoted(int count);

	public void Add(T item, bool duplicates)
	{
		if (prefix is not null)
		{
			AddToPrefix(item, duplicates);
			return;
		}

		int rank;
		try
		{
			if (last < 0 || comparer.Compare(Entry(last).Item, item) < 0)
			{
				rank = Count;
			}
			else if (!duplicates)
			{
				var found = Search(item);
				if (found >= 0)
				{
					return;
				}

				rank = ~found;
			}
			else
			{
				var lower = Bound(item, upper: false);
				var upper = Bound(item, upper: true);
				rank = lower;
				if (lower < upper)
				{
					var low = 0;
					var high = Count - 1;
					while (low <= high)
					{
						Visit();
						var middle = low + ((high - low) >> 1);
						if (middle < lower)
						{
							low = middle + 1;
						}
						else if (middle >= upper)
						{
							high = middle - 1;
						}
						else
						{
							rank = middle;
							break;
						}
					}
				}
			}
		}
		catch (Exception exception)
		{
			throw new InvalidOperationException("Failed to compare two elements in the array.", exception);
		}

		Insert(rank, item);
	}

	private void AddToPrefix(T item, bool duplicates)
	{
		var items = prefix!;
		var comparison = -1;
		if (items.Count > 0)
		{
			try
			{
				comparison = comparer.Compare(items[^1], item);
			}
			catch (Exception exception)
			{
				throw new InvalidOperationException("Failed to compare two elements in the array.", exception);
			}
		}

		if (comparison < 0)
		{
			if (items.Count == items.Capacity)
			{
				Grow(items.Count);
			}

			items.Add(item);
			return;
		}

		if (comparison == 0 && !duplicates)
		{
			return;
		}

		var found = items.BinarySearch(item, comparer);
		if (found >= 0 && !duplicates)
		{
			return;
		}

		var rank = found < 0 ? ~found : found;
		PromotePrefix(items);
		Insert(rank, item);
	}

	private void PromotePrefix(List<T> items)
	{
		nodes.EnsureCapacity(items.Count + 1);
		Promoted(items.Count);
		for (var i = 0; i < items.Count; i++)
		{
			Visit();
			nodes.Add(new(items[i]) { Previous = i - 1, Next = i == items.Count - 1 ? -1 : i + 1 });
		}

		root = Build(0, items.Count);
		first = 0;
		last = items.Count - 1;
		prefix = null;
		Volatile.Write(ref cursor, -1);
	}

	private int Build(int start, int end)
	{
		if (start == end)
		{
			return -1;
		}

		var middle = start + ((end - start) >> 1);
		ref var node = ref Entry(middle);
		node.Left = Build(start, middle);
		node.Right = Build(middle + 1, end);
		Update(middle);
		return middle;
	}

	public int IndexOf(T item)
	{
		if (prefix is not null)
		{
			var rank = prefix.BinarySearch(item, comparer);
			return rank < 0 ? -1 : rank;
		}

		int found;
		try
		{
			found = Search(item);
		}
		catch (Exception exception)
		{
			throw new InvalidOperationException("Failed to compare two elements in the array.", exception);
		}

		return found < 0 ? -1 : found;
	}

	private int Search(T item)
	{
		var current = root;
		var rank = 0;
		while (current >= 0)
		{
			ref var node = ref Entry(current);
			var comparison = comparer.Compare(node.Item, item);
			if (comparison == 0)
			{
				return rank + Size(node.Left);
			}

			if (comparison > 0)
			{
				current = node.Left;
			}
			else
			{
				rank += Size(node.Left) + 1;
				current = node.Right;
			}
		}

		return ~rank;
	}

	private int Bound(T item, bool upper)
	{
		var current = root;
		var rank = 0;
		while (current >= 0)
		{
			ref var node = ref Entry(current);
			var comparison = comparer.Compare(node.Item, item);
			if (comparison < 0 || (upper && comparison == 0))
			{
				rank += Size(node.Left) + 1;
				current = node.Right;
			}
			else
			{
				current = node.Left;
			}
		}

		return rank;
	}

	private int FindNode(int index)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

		int current;
		var cached = Volatile.Read(ref cursor);
		var cachedRank = (int)(cached >> 32);
		var distance = index - cachedRank;
		if (index == 0)
		{
			current = first;
		}
		else if (index == Count - 1)
		{
			current = last;
		}
		else if (cached >= 0 && distance >= -2 && distance <= 2)
		{
			current = (int)cached;
			while (distance > 0)
			{
				current = Entry(current).Next;
				distance--;
			}

			while (distance < 0)
			{
				current = Entry(current).Previous;
				distance++;
			}
		}
		else
		{
			current = root;
			var remaining = index;
			while (true)
			{
				ref var node = ref Entry(current);
				var left = Size(node.Left);
				if (remaining == left)
				{
					break;
				}

				if (remaining < left)
				{
					current = node.Left;
				}
				else
				{
					remaining -= left + 1;
					current = node.Right;
				}
			}
		}

		Volatile.Write(ref cursor, ((long)index << 32) | (uint)current);
		return current;
	}

	private void Insert(int rank, T item)
	{
		var next = rank == Count ? -1 : FindNode(rank);
		var previous = next < 0 ? last : Entry(next).Previous;
		var added = Count;
		if (nodes.Count == nodes.Capacity)
		{
			Grow(nodes.Count);
		}

		nodes.Add(new(item) { Previous = previous, Next = next });
		if (previous < 0)
		{
			first = added;
		}
		else
		{
			Entry(previous).Next = added;
		}

		if (next < 0)
		{
			last = added;
		}
		else
		{
			Entry(next).Previous = added;
		}

		root = InsertAt(root, rank, added);
		Volatile.Write(ref cursor, -1);
	}

	private int InsertAt(int current, int rank, int added)
	{
		if (current < 0)
		{
			return added;
		}

		ref var node = ref Entry(current);
		var left = Size(node.Left);
		if (rank <= left)
		{
			node.Left = InsertAt(node.Left, rank, added);
		}
		else
		{
			node.Right = InsertAt(node.Right, rank - left - 1, added);
		}

		Update(current);
		var balance = Height(node.Left) - Height(node.Right);
		if (balance > 1)
		{
			ref var child = ref Entry(node.Left);
			if (Height(child.Left) < Height(child.Right))
			{
				node.Left = RotateLeft(node.Left);
			}

			return RotateRight(current);
		}

		if (balance < -1)
		{
			ref var child = ref Entry(node.Right);
			if (Height(child.Right) < Height(child.Left))
			{
				node.Right = RotateRight(node.Right);
			}

			return RotateLeft(current);
		}

		return current;
	}

	private int Size(int index) => index < 0 ? 0 : Entry(index).Size;

	private int Height(int index) => index < 0 ? 0 : Entry(index).Height;

	private void Update(int index)
	{
		ref var node = ref Entry(index);
		node.Size = Size(node.Left) + Size(node.Right) + 1;
		node.Height = Math.Max(Height(node.Left), Height(node.Right)) + 1;
	}

	private int RotateLeft(int index)
	{
		ref var node = ref Entry(index);
		var replacement = node.Right;
		ref var other = ref Entry(replacement);
		node.Right = other.Left;
		other.Left = index;
		Update(index);
		Update(replacement);
		return replacement;
	}

	private int RotateRight(int index)
	{
		ref var node = ref Entry(index);
		var replacement = node.Left;
		ref var other = ref Entry(replacement);
		node.Left = other.Right;
		other.Right = index;
		Update(index);
		Update(replacement);
		return replacement;
	}

	public void Clear()
	{
		if (prefix is null)
		{
			nodes.Clear();
			prefix = [];
		}
		else
		{
			prefix.Clear();
			nodes.Capacity = 0;
		}
		root = -1;
		first = -1;
		last = -1;
		Volatile.Write(ref cursor, -1);
	}
}
