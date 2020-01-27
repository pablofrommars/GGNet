using System;
using System.Collections.Generic;

namespace GGNet
{
    using Scales;
    using Facets;
    using Geoms;

    public partial class Data<T, TX, TY>
    {
        public class Panel
        {
            public Panel((int row, int col) coord, Data<T, TX, TY> data, double width, double height)
            {
                Coord = coord;
                Data = data;
                Width = width;
                Height = height;

                Id = $"{coord.row}_{coord.col}";
            }

            public (int row, int col) Coord { get; }

            public Data<T, TX, TY> Data { get; }

            public double Width { get; }

            public double Height { get; }

            public string Id { get; }

            public Buffer<IGeom> Geoms { get; } = new Buffer<IGeom>(8, 1);

            internal (string x, string y) Strip { get; set; }

            internal (bool x, bool y) Axis { get; set; }

            internal (double height, string text) XLab { get; set; }

            internal (double width, string text) YLab { get; set; }

            internal Components.Panel<T, TX, TY> Component { get; set; }

            internal void Register(Components.Panel<T, TX, TY> component)
            {
                Component = component;
            }

            internal Position<TX> X
            {
                get
                {
                    if (Data.Positions.X.Scales.Count == 1)
                    {
                        return Data.Positions.X.Scales[0];
                    }
                    else
                    {
                        return Data.Positions.X.Scales[Coord.row * Data.N.cols + Coord.col];
                    }
                }
            }

            internal Position<TY> Y
            {
                get
                {
                    if (Data.Positions.Y.Scales.Count == 1)
                    {
                        return Data.Positions.Y.Scales[0];
                    }
                    else
                    {
                        return Data.Positions.Y.Scales[Coord.row * Data.N.cols + Coord.col];
                    }
                }
            }
        }

        public class PanelFactory
        {
            private readonly List<Func<Panel, Facet<T>, IGeom>> geoms = new List<Func<Panel, Facet<T>, IGeom>>();

            public PanelFactory(Data<T, TX, TY> data, double width = 1, double height = 1)
            {
                Data = data;
                Width = width;
                Height = height;
            }

            public Data<T, TX, TY> Data { get; }

            public double Width { get; }

            public double Height { get; }

            public Func<Position<TY>> Y { get; set; }

            public string YLab { get; set; }

            public void Add(Func<Panel, Facet<T>, IGeom> geom) => geoms.Add(geom);

            public Panel Build((int, int) coord, Facet<T> facet = null, double? width = null, double? height = null)
            {
                var panel = new Panel(coord, Data, width ?? Width, height ?? Height);

                foreach (var geom in geoms)
                {
                    panel.Geoms.Add(geom(panel, facet));
                }

                return panel;
            }
        }
    }
}
