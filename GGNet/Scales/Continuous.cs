using GGNet.Transformations;

namespace GGNet.Scales
{
    public abstract class Continuous<TKey> : Scale<TKey, double>
        where TKey : struct
    {
        public Continuous(ITransformation<TKey> transformation)
            : base(transformation)
        {
        }

        public (TKey? min, TKey? max) Limits { get; set; }
    }
}
