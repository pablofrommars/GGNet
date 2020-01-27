using System.Collections.Generic;

namespace GGNet.Formats
{
    public class Labeller<T> : IFormatter<T>
    {
        private readonly IDictionary<T, string> labels;

        public Labeller() => labels = new Dictionary<T, string>();

        public Labeller(IDictionary<T, string> labels) => this.labels = labels;

        public string this[T key]
        {
            get => labels[key];
            set => labels[key] = value;
        }

        public string Format(T value) => labels[value];
    }
}
