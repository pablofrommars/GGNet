namespace GGNet.Buffers;

public abstract class BufferBase<T>
{
	protected readonly IComparer<T> comparer;

	protected T[][] pages;

	protected int pageCapacity;
	protected int pagesIncrement;
	protected int pagesCapacity;

	protected int count;
	protected int page;
	protected int element;

	public BufferBase(int pageCapacity = 32, int pagesIncrement = 4, IComparer<T>? comparer = null)
	{
		this.comparer = comparer ?? Comparer<T>.Default;

		pages = new T[pagesIncrement][];
		pages[0] = new T[pageCapacity];

		this.pageCapacity = pageCapacity;
		this.pagesIncrement = pagesIncrement;
		pagesCapacity = pagesIncrement;

		count = 0;
		page = 0;
		element = 0;
	}

	public int Count => count;

	protected T Get(int i) => pages[i / pageCapacity][i % pageCapacity];

	protected void Set(int i, T item) => pages[i / pageCapacity][i % pageCapacity] = item;

	public T this[int i]
	{
		get => Get(i);
		set => Set(i, value);
	}

	public abstract void Add(T item);

	public void Add(IEnumerable<T> items)
	{
		foreach (var item in items)
		{
			Add(item);
		}
	}

	protected void Grow()
	{
		count++;

		if (element == pageCapacity)
		{
			page++;

			if (page == pagesCapacity)
			{
				pagesCapacity += pagesIncrement;
				Array.Resize(ref pages, pagesCapacity);
			}

			pages[page] ??= new T[pageCapacity];

			element = 0;
		}
	}

	protected void Append(T item)
	{
		Grow();
		pages[page][element++] = item;
	}

	public abstract int IndexOf(T item);

	public void Clear()
	{
		count = 0;
		page = 0;
		element = 0;
	}
}
