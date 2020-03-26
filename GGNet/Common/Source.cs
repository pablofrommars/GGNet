using System.Collections.Generic;

namespace GGNet
{
    public class Source<T> : Buffer<T>
    {
        public Source()
            : base()
        {
        }

        public Source(IEnumerable<T> items)
            : this()
        {
            Add(items);
        }
    }
}
