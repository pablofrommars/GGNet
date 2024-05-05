namespace GGNet.Buffers;

public sealed class SortedBuffer<T>(int pageCapacity = 32, int pagesIncrement = 4, IComparer<T>? comparer = null) : BufferBase<T>(pageCapacity, pagesIncrement, comparer)
{
  public override void Add(T item)
  {
    if (Count == 0)
    {
      Append(item);
      return;
    }

    var cmp = comparer.Compare(pages[page][element - 1], item);
    if (cmp == 0)
    {
      return;
    }
    else if (cmp < 0)
    {
      Append(item);
      return;
    }

    var found = false;
    var p = 0;
    var i = 0;

    while (p <= page)
    {
      i = 0;

      while (i < pageCapacity)
      {
        cmp = comparer.Compare(item, pages[p][i]);
        if (cmp == 0)
        {
          return;
        }
        if (cmp < 0)
        {
          found = true;
          break;
        }

        i++;
      }

      if (found)
      {
        break;
      }

      p++;
    }

    Grow();

    while (p <= page)
    {
      while (i < pageCapacity)
      {
        (item, pages[p][i]) = (pages[p][i], item);
        i++;
      }

      i = 0;
      p++;
    }

    element++;
  }

  public override int IndexOf(T item)
  {
    var start = 0;
    var n = Count;

    while (n > 0)
    {
      var m = start + (n - 1) / 2;
      var mvalue = this[m];

      var cmp = comparer.Compare(item, mvalue);
      if (cmp == 0)
      {
        return m;
      }
      else if (cmp > 0)
      {
        start = m + 1;
      }

      n /= 2;
    }

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

    return -1;
  }
}
