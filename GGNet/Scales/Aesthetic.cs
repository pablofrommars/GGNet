using System;
using System.Collections.Generic;

namespace GGNet.Scales
{
    public interface IAestheticMapping
    {
        bool Guide { get; }

        string Name { get; }
    }

    public interface IAestheticMapping<T, TValue> : IAestheticMapping
    {
        void Train(T item);

        TValue Map(T item);

        IEnumerable<(TValue value, string label)> Labels { get; }
    }

    public class Aesthetic<T, TKey, TValue> : IAestheticMapping<T, TValue>
    {
        private readonly Func<T, TKey> selector;
        private readonly Scale<TKey, TValue> scale;

        public Aesthetic(Func<T, TKey> selector, Scale<TKey, TValue> scale, bool guide, string name)
        {
            this.selector = selector;
            this.scale = scale;

            Guide = guide;
            Name = name;
        }

        public bool Guide { get; }

        public string Name { get; }

        public void Train(T item) => scale.Train(selector(item));

        public TValue Map(T item) => scale.Map(selector(item));

        public IEnumerable<(TValue value, string label)> Labels => scale.Labels;
    }
}
