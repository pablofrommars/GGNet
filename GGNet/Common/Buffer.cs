using System;
using System.Collections.Generic;

namespace GGNet
{
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

        public BufferBase(int pageCapacity = 32, int pagesIncrement = 4, IComparer<T> comparer = null)
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

    public class Buffer<T> : BufferBase<T>
    {
        public Buffer(int pageCapacity = 32, int pagesIncrement = 4)
            : base(pageCapacity, pagesIncrement) 
        { 
        }
        
        public override void Add(T item) => Append(item);

        public void Add(Buffer<T> buffer)
        {
            for (var i = 0; i < buffer.Count; i++)
            {
                Add(buffer[i]);
            }
        }

        public override int IndexOf(T item)
        {
            var i = 0;

            for (var p = 0; p < page; p++)
            {
                for (var j = 0; j < pageCapacity; j++)
                {
                    if (comparer.Compare(pages[p][j], item) == 0)
                    {
                        return i;
                    }

                    i++;
                }
            }

            return -1;
        }
    }

    public class SortedBuffer<T> : BufferBase<T>
    {
        public SortedBuffer(int pageCapacity = 32, int pagesIncrement = 4, IComparer<T> comparer = null)
            : base(pageCapacity, pagesIncrement, comparer)
        {
        }

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
                    var tmp = pages[p][i];
                    pages[p][i] = item;
                    item = tmp;

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
}
