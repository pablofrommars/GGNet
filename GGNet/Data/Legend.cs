using static System.Math;

namespace GGNet
{
    using Elements;
    using Scales;

    using static Direction;

    internal class Legend
    {
        public Legend(Theme theme, IAestheticMapping aes)
        {
            Aes = aes;

            if (!string.IsNullOrEmpty(aes.Name))
            {
                Title = new Dimension<string>
                {
                    Value = aes.Name,
                    Width = aes.Name.Width(theme.Legend.Title.Size),
                    Height = aes.Name.Height(theme.Legend.Title.Size)
                };
            }

            Items = new _Items(theme);
        }

        public IAestheticMapping Aes { get; }

        public class Dimension<TValue>
        {
            public TValue Value { get; set; }

            public double Width { get; set; }

            public double Height { get; set; }
        }

        public Dimension<string> Title { get; }

        internal class Elements : Buffer<Dimension<IElement>>
        {
            private readonly double size;

            public Elements(double size) : base(4, 1) => this.size = size;

            public double Width { get; set; }

            public double Height { get; set; }

            public Dimension<IElement> Add(IElement element)
            {
                var dim = new Dimension<IElement>
                {
                    Value = element,
                    Width = size,
                    Height = size
                };

                if (element is Circle c)
                {
                    var diam = 2 * c.Radius;

                    dim.Width = Max(dim.Width, diam);
                    dim.Height = Max(dim.Height, diam);
                }

                Width = Max(Width, dim.Width);
                Height = Max(Height, dim.Height);

                Add(dim);

                return dim;
            }
        }

        internal class _Items : Buffer<(Dimension<string> label, Elements elements)>
        {
            private readonly Theme theme;

            public _Items(Theme theme) : base(8, 1) => this.theme = theme;

            public (Dimension<string> label, Elements elements) GetOrAdd(string label)
            {
                for (int i = 0; i < Count; i++)
                {
                    var ret = Get(i);
                    if (ret.label.Value == label)
                    {
                        return ret;
                    }
                }

                var height = label.Height(theme.Legend.Labels.Size);

                var item = (
                    label: new Dimension<string>
                    {
                        Value = label,
                        Width = label.Width(theme.Legend.Labels.Size),
                        Height = height
                    },
                    elements: new Elements(height)
                );

                Add(item);

                return item;
            }
        }

        public _Items Items { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public void Add(string label, IElement element)
        {
            var dim = Items.GetOrAdd(label).elements.Add(element);

            Width = Max(Width, dim.Width);
            Height = Max(Height, dim.Height);
        }
    }

    internal class Legends : Buffer<Legend>
    {
        private readonly Theme theme;

        public Legends(Theme theme) : base(8, 1) => this.theme = theme;

        public Legend GetOrAdd(IAestheticMapping aes)
        {
            for (int i = 0; i < Count; i++)
            {
                var ret = Get(i);
                if (ret.Aes == aes)
                {
                    return ret;
                }
            }

            var legend = new Legend(theme, aes);

            Add(legend);

            return legend;
        }

        public (double width, double height) Dimension()
        {
            var width = 0.0;
            var height = 0.0;

            for (int i = 0; i < Count; i++)
            {
                var legend = Get(i);

                if (legend.Title?.Width > 0)
                {
                    var w = theme.Legend.Title.Margin.Left + legend.Title.Width + theme.Legend.Title.Margin.Left;
                    var h = theme.Legend.Title.Margin.Top + legend.Title.Height + theme.Legend.Title.Margin.Bottom;

                    if (theme.Legend.Direction == Vertical)
                    {
                        width = Max(width, w);
                        height += h;
                    }
                    else
                    {
                        width += w;
                        height = Max(height, h);
                    }
                }

                for (int j = 0; j < legend.Items.Count; j++)
                {
                    var (label, elements) = legend.Items[j];

                    var w = legend.Width + theme.Legend.Labels.Margin.Left + label.Width + theme.Legend.Labels.Margin.Right;
                    var h = theme.Legend.Labels.Margin.Top + Max(elements.Height, label.Height) + theme.Legend.Labels.Margin.Bottom;

                    if (theme.Legend.Direction == Vertical)
                    {
                        width = Max(width, w);
                        height += h;
                    }
                    else
                    {
                        width += w;
                        height = Max(height, h);
                    }
                }
            }

            return (width, height);
        }
    }
}
