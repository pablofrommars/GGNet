using System.Collections.Generic;
using System.Linq;

namespace GGNet.Palettes
{
    public class Discrete<TKey, TValue>
    {
        private readonly TValue[] palette;
        private readonly int direction;

        private int i = 0;
        private readonly Dictionary<TKey, (int i, TValue value)> map = new Dictionary<TKey, (int i, TValue value)>();

        public Discrete(TValue[] palette, int direction = 1)
        {
            this.palette = palette;
            this.direction = direction;
        }

        public TValue this[TKey key]
        {
            get => map[key].value;
        }

        public void Add(TKey key)
        {
            if (map.ContainsKey(key))
            {
                return;
            }

            map[key] = (i++, default);
        }

        public void Set()
        {
            var sub = Utils.Sample(palette, map.Count, direction);

            foreach (var (key, i) in map.Select(o => (o.Key, o.Value.i)).ToList())
            {
                map[key] = (i, sub[i]);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default;

            if (!map.TryGetValue(key, out var o))
            {
                return false;
            }

            value = o.value;

            return true;
        }

        public (TValue value, TKey key)[] Values()
        {
            var values = new (TValue value, TKey key)[map.Count];

            foreach(var (k, o) in map)
            {
                values[o.i] = (o.value, k);
            }

            return values;
        }

        public void Clear()
        {
            i = 0;
            map.Clear();
        }

        public static Discrete<TKey, TValue> New(TKey[] keys, TValue[] palette, int direction = 1)
        {
            var discrete = new Discrete<TKey, TValue>(palette, direction);

            for (var i = 0; i < keys.Length; i++)
            {
                discrete.Add(keys[i]);
            }

            discrete.Set();

            return discrete;
        }

        public static Discrete<TKey, TValue> Enum(TValue[] palette, int direction = 1) => New((TKey[])System.Enum.GetValues(typeof(TKey)), palette, direction);
    }
}
