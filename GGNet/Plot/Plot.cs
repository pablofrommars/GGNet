using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using NodaTime;

namespace GGNet
{
    using Elements;
    using Scales;
    using Facets;
    using Geoms;
    using Transformations;
    using Formats;

    using static Position;

    public static class Plot
    {
        private static Data<T, TX, TY> _New<T, TX, TY>(Source<T> source, Func<T, TX> x, Func<T, TY> y)
            where TX : struct
            where TY : struct
        {
            var data = new Data<T, TX, TY>()
            {
                Source = source,
            };

            data.Selectors.X = x;
            data.Selectors.Y = y;

            return data;
        }

        public static Data<T, LocalDate, double> New<T>(Source<T> source, Func<T, LocalDate> x, Func<T, double> y = null) => _New(source, x, y);

        public static Data<T, LocalDateTime, double> New<T>(Source<T> source, Func<T, LocalDateTime> x, Func<T, double> y = null) => _New(source, x, y);

        public static Data<T, TX, double> New<T, TX>(Source<T> source, Func<T, TX> x, Func<T, double> y = null)
            where TX : struct, Enum
        {
            return _New(source, x, y);
        }

        public static Data<T, TX, TY> New<T, TX, TY>(Source<T> source, Func<T, TX> x, Func<T, TY> y)
            where TX : struct, Enum
            where TY : struct, Enum
        {
            return _New(source, x, y);
        }

        public static Data<T, double, TY> New<T, TY>(Source<T> source, Func<T, double> x, Func<T, TY> y)
            where TY : struct, Enum
        {
            return _New(source, x, y);
        }

        public static Data<T, double, double> New<T>(Source<T> source, Func<T, double> x = null, Func<T, double> y = null) => _New(source, x, y);

        public static Data<IWaiver, double, double> New() => _New<IWaiver, double, double>(null, null, null);

        public static Data<T, LocalDate, TY> Scale_X_Discrete_Date<T, TY>(
            this Data<T, LocalDate, TY> data,
            (LocalDate? min, LocalDate? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
            where TY : struct
        {
            data.Positions.X.Factory = () => new DiscretDates(null, limits, expand);

            return data;
        }

        public static Data<T, LocalDateTime, TY> Scale_X_Discrete_DateTime<T, TY>(
            this Data<T, LocalDateTime, TY> data,
            (LocalDateTime? min, LocalDateTime? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
            where TY : struct
        {
            data.Positions.X.Factory = () => new DateTimePosition(null, limits, expand);

            return data;
        }

        public static Data<T, double, TY> Scale_X_Continuous<T, TY>(
            this Data<T, double, TY> data,
            ITransformation<double> transformation = null,
            (double? min, double? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
            string format = null)
            where TY : struct
        {
            data.Positions.X.Factory = () => new Extended(transformation, limits, expand, format);

            return data;
        }

        public static Data<T, TX, TY> Scale_X_Discrete<T,TX, TY>(
           this Data<T, TX, TY> data,
           (TX? min, TX? max)? limits = null,
           (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
           IFormatter<TX> formatter = null)
           where TX : struct
           where TY : struct
        {
            data.Positions.X.Factory = () => new DiscretePosition<TX>(null, limits, expand, formatter);

            return data;
        }

        public static Data<T, double, TY> XLim<T, TY>(this Data<T, double, TY> data, double? min = null, double? max = null)
            where TY : struct
        {
            if (data.Positions.X.Factory == null)
            {
                data.Scale_X_Continuous();
            }

            var old = data.Positions.X.Factory;

            data.Positions.X.Factory = () =>
            {
                var scale = old();

                scale.Limits = (min, max);

                return scale;
            };

            return data;
        }

        public static Data<T, LocalDate, TY> XLim<T, TY>(this Data<T, LocalDate, TY> data, LocalDate? min = null, LocalDate? max = null)
            where TY : struct
        {
            if (data.Positions.X.Factory == null)
            {
                data.Scale_X_Discrete_Date();
            }

            var old = data.Positions.X.Factory;

            data.Positions.X.Factory = () =>
            {
                var scale = old();

                scale.Limits = (min, max);

                return scale;
            };

            return data;
        }

        public static Data<T, TX, double> Scale_Y_Continuous<T, TX>(
            this Data<T, TX, double> data,
            ITransformation<double> transformation = null,
            (double? min, double? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
            string format = null)
            where TX : struct
        {
            data.Positions.Y.Factory = () => new Extended(transformation, limits, expand, format);

            return data;
        }

        public static Data<T, TX, double>.PanelFactory Scale_Y_Continuous<T, TX>(
            this Data<T, TX, double>.PanelFactory panel,
             ITransformation<double> transformation = null,
            (double? min, double? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
            string format = null)
            where TX : struct
        {
            panel.Y = () => new Extended(transformation, limits, expand, format);

            return panel;
        }

        public static Data<T, double, TY> Scale_X_Sqrt<T, TY>(
            this Data<T, double, TY> data,
            (double? min, double? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
            string format = null)
            where TY : struct
            => data.Scale_X_Continuous(Sqrt.Instance, limits, expand, format);

        public static Data<T, TX, double>.PanelFactory Scale_Y_Sqrt<T, TX>(
            this Data<T, TX, double>.PanelFactory panel,
            (double? min, double? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
            string format = null)
            where TX : struct
            => panel.Scale_Y_Continuous(Sqrt.Instance, limits, expand, format);

        public static Data<T, TX, double> Scale_Y_Sqrt<T, TX>(
            this Data<T, TX, double> data,
            (double? min, double? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
            string format = null)
            where TX : struct
            => data.Scale_Y_Continuous(Sqrt.Instance, limits, expand, format);

        public static Data<T, TX, double>.PanelFactory Scale_Y_Log10<T, TX>(
            this Data<T, TX, double>.PanelFactory panel,
            (double? min, double? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
            string format = null)
            where TX : struct
            => panel.Scale_Y_Continuous(Log10.Instance, limits, expand, format);

        public static Data<T, TX, double> Scale_Y_Log10<T, TX>(
            this Data<T, TX, double> data,
            (double? min, double? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
            string format = null)
            where TX : struct
            => data.Scale_Y_Continuous(Log10.Instance, limits, expand, format);

        public static Data<T, TX, TY> Scale_Y_Discrete<T, TX, TY>(
           this Data<T, TX, TY> data,
           (TY? min, TY? max)? limits = null,
           (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
           IFormatter<TY> formatter = null)
           where TX : struct
           where TY : struct
        {
            data.Positions.Y.Factory = () => new DiscretePosition<TY>(null, limits, expand, formatter);

            return data;
        }

        public static Data<T, TX, double> YLim<T, TX>(this Data<T, TX, double> data, double? min = null, double? max = null)
           where TX : struct
        {
            if (data.Positions.Y.Factory == null)
            {
                data.Scale_Y_Continuous();
            }

            var old = data.Positions.Y.Factory;

            data.Positions.Y.Factory = () =>
            {
                var scale = old();

                scale.Limits = (min, max);

                return scale;
            };

            return data;
        }

        public static Data<T, TX, double>.PanelFactory YLim<T, TX>(this Data<T, TX, double>.PanelFactory panel, double? min = null, double? max = null)
            where TX : struct
        {
            if (panel.Y == null)
            {
                panel.Scale_Y_Continuous();
            }

            var old = panel.Y;

            panel.Y = () =>
            {
                var scale = old();

                scale.Limits = (min, max);

                return scale;
            };

            return panel;
        }

        internal static Data<T, TX, TY>.PanelFactory Default_Panel<T, TX, TY>(this Data<T, TX, TY> data)
            where TX : struct
            where TY : struct
        {
            data.DefaultFactory ??= new Data<T, TX, TY>.PanelFactory(data);

            return data.DefaultFactory;
        }

        public static Data<T, TX, TY> Panel<T, TX, TY>(this Data<T, TX, TY> data, Func<Data<T, TX, TY>.PanelFactory, Data<T, TX, TY>.PanelFactory> factory, double width = 1.0, double height = 1.0)
            where TX : struct
            where TY : struct
        {
            var panel = factory(new Data<T, TX, TY>.PanelFactory(data, width, height));

            data.PanelFactories.Add(panel);

            return data;
        }

        public static Data<T, TX, TY> Title<T, TX, TY>(this Data<T, TX, TY> data, string title)
            where TX : struct
            where TY : struct
        {
            data.Title = title;

            return data;
        }

        public static Data<T, TX, TY> SubTitle<T, TX, TY>(this Data<T, TX, TY> data, string subtitle)
            where TX : struct
            where TY : struct
        {
            data.SubTitle = subtitle;

            return data;
        }

        public static Data<T, TX, TY> Caption<T, TX, TY>(this Data<T, TX, TY> data, string caption)
            where TX : struct
            where TY : struct
        {
            data.Caption = caption;

            return data;
        }

        public static Data<T, TX, TY> XLab<T, TX, TY>(this Data<T, TX, TY> data, string xlab)
            where TX : struct
            where TY : struct
        {
            data.XLab = xlab;

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory YLab<T, TX, TY>(this Data<T, TX, TY>.PanelFactory panel, string ylab)
            where TX : struct
            where TY : struct
        {
            panel.YLab = ylab;

            return panel;
        }

        public static Data<T, TX, TY> YLab<T, TX, TY>(this Data<T, TX, TY> data, string ylab)
            where TX : struct
            where TY : struct
        {

            data.Default_Panel().YLab(ylab);

            return data;
        }

        public static Data<T, TX, TY> Flip<T, TX, TY>(this Data<T, TX, TY> data)
            where TX : struct
            where TY : struct
        {
            data.Flip = true;

            return data;
        }

        internal static Data<T1, TX1, TY1>.PanelFactory Add_Geom<T1, TX1, TY1, T2, TX2, TY2>(this Data<T1, TX1, TY1>.PanelFactory panel, Func<Geom<T2, TX2, TY2>> builder)
            where TX1 : struct
            where TY1 : struct
            where TX2 : struct
            where TY2 : struct
        {
            panel.Add((p, f) =>
            {
                var geom = builder();

                geom.Init(p, f);

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_Point<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            IAestheticMapping<T2, double> _size = null,
            IAestheticMapping<T2, string> _color = null,
            Func<T2, MouseEventArgs, Task> onclick = null,
            Func<T2, MouseEventArgs, Task> onmouseover = null,
            Func<T2, MouseEventArgs, Task> onmouseout = null,
            Func<T2, string[]> tooltip = null,
            double size = 5, string color = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new Point<T2, TX2, TY2>(source, x, y, _size, _color, tooltip, inherit)
                {
                    OnClick = onclick,
                    OnMouseOver = onmouseover,
                    OnMouseOut = onmouseout,
                    Aesthetic = new Circle
                    {
                        Radius = size,
                        Fill = color,
                        Alpha = alpha,
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_Point<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            IAestheticMapping<T2, double> _size = null,
            IAestheticMapping<T2, string> _color = null,
            Func<T2, MouseEventArgs, Task> onclick = null,
            Func<T2, MouseEventArgs, Task> onmouseover = null,
            Func<T2, MouseEventArgs, Task> onmouseout = null,
            Func<T2, string[]> tooltip = null,
            double size = 5, string color = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_Point(source, x, y, _size, _color, onclick, onmouseover, onmouseout, tooltip, size, color, alpha, inherit);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_Point<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            IAestheticMapping<T, double> _size = null,
            IAestheticMapping<T, string> _color = null,
            Func<T, MouseEventArgs, Task> onclick = null,
            Func<T, MouseEventArgs, Task> onmouseover = null,
            Func<T, MouseEventArgs, Task> onmouseout = null,
            Func<T, string[]> tooltip = null,
            double size = 5, string color = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            return Geom_Point(panel, panel.Data.Source, x, y, _size, _color, onclick, onmouseover, onmouseout, tooltip, size, color, alpha, inherit);
        }

        public static Data<T, TX, TY> Geom_Point<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            IAestheticMapping<T, double> _size = null,
            IAestheticMapping<T, string> _color = null,
            Func<T, MouseEventArgs, Task> onclick = null,
            Func<T, MouseEventArgs, Task> onmouseover = null,
            Func<T, MouseEventArgs, Task> onmouseout = null,
            Func<T, string[]> tooltip = null,
            double size = 5, string color = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_Point(x, y, _size, _color, onclick, onmouseover, onmouseout, tooltip, size, color, alpha, inherit);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_Line<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            IAestheticMapping<T2, string> _color = null,
            IAestheticMapping<T2, LineType> _lineType = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new Line<T2, TX2, TY2>(source, x, y, _color, _lineType, inherit)
                {
                    Aesthetic = new Line
                    {
                        Width = width,
                        Color = color,
                        Alpha = alpha,
                        LineType = lineType
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_Line<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            IAestheticMapping<T2, string> _color = null,
            IAestheticMapping<T2, LineType> _lineType = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_Line(source, x, y, _color, _lineType, width, color, alpha, lineType, inherit);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_Line<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            IAestheticMapping<T, string> _color = null,
            IAestheticMapping<T, LineType> _lineType = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            return Geom_Line(panel, panel.Data.Source, x, y, _color, _lineType, width, color, alpha, lineType, inherit);
        }

        public static Data<T, TX, TY> Geom_Line<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            IAestheticMapping<T, string> _color = null,
            IAestheticMapping<T, LineType> _lineType = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_Line(x, y, _color, _lineType, width, color, alpha, lineType, inherit);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_Bar<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            IAestheticMapping<T2, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            PositionAdjustment position = PositionAdjustment.Stack,
            double width = 0.9,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new Bar<T2, TX2, TY2>(source, x, y, _fill, position, width, inherit)
                {
                    Aesthetic = new Rectangle
                    {
                        Fill = fill,
                        Alpha = alpha
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_Bar<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            IAestheticMapping<T2, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            PositionAdjustment position = PositionAdjustment.Stack,
            double width = 0.9,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_Bar(source, x, y, _fill, fill, alpha, position, width, inherit);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_Bar<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            IAestheticMapping<T, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            PositionAdjustment position = PositionAdjustment.Stack,
            double width = 0.9,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            return Geom_Bar(panel, panel.Data.Source, x, y, _fill, fill, alpha, position, width, inherit);
        }

        public static Data<T, TX, TY> Geom_Bar<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            IAestheticMapping<T, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            PositionAdjustment position = PositionAdjustment.Stack,
            double width = 0.9,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_Bar(x, y, _fill, fill, alpha, position, width, inherit);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_Segment<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x,
            Func<T2, TX2> xend,
            Func<T2, TY2> y,
            Func<T2, TY2> yend,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new Segment<T2, TX2, TY2>(source, x, xend, y, yend)
                {
                    Aesthetic = new Line
                    {
                        Width = width,
                        Color = color,
                        Alpha = alpha,
                        LineType = lineType
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_Segment<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x,
            Func<T2, TX2> xend,
            Func<T2, TY2> y,
            Func<T2, TY2> yend,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_Segment(source, x, xend, y, yend, width, color, alpha, lineType, inherit);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_Segment<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x,
            Func<T, TX> xend,
            Func<T, TY> y,
            Func<T, TY> yend,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            return Geom_Segment(panel, panel.Data.Source, x, xend, y, yend, width, color, alpha, lineType, inherit);
        }

        public static Data<T, TX, TY> Geom_Segment<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x,
            Func<T, TX> xend,
            Func<T, TY> y,
            Func<T, TY> yend,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_Segment(x, xend, y, yend, width, color, alpha, lineType, inherit);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_Area<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            IAestheticMapping<T2, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new Area<T2, TX2, TY2>(source, x, y, _fill, inherit)
                {
                    Aesthetic = new Rectangle
                    {
                        Fill = fill,
                        Alpha = alpha
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_Area<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            IAestheticMapping<T2, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_Area(source, x, y, _fill, fill, alpha, inherit);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_Area<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            IAestheticMapping<T, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            return Geom_Area(panel, panel.Data.Source, x, y, _fill, fill, alpha, inherit);
        }

        public static Data<T, TX, TY> Geom_Area<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            IAestheticMapping<T, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_Area(x, y, _fill, fill, alpha, inherit);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_Ribbon<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> ymin = null,
            Func<T2, TY2> ymax = null,
            IAestheticMapping<T2, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new Ribbon<T2, TX2, TY2>(source, x, ymin, ymax, _fill, inherit)
                {
                    Aesthetic = new Rectangle
                    {
                        Fill = fill,
                        Alpha = alpha
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_Ribbon<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> ymin = null,
            Func<T2, TY2> ymax = null,
            IAestheticMapping<T2, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_Ribbon(source, x, ymin, ymax, _fill, fill, alpha, inherit);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_Ribbon<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> ymin = null,
            Func<T, TY> ymax = null,
            IAestheticMapping<T, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            return Geom_Ribbon(panel, panel.Data.Source, x, ymin, ymax, _fill, fill, alpha, inherit);
        }

        public static Data<T, TX, TY> Geom_Ribbon<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> ymin = null,
            Func<T, TY> ymax = null,
            IAestheticMapping<T, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_Ribbon(x, ymin, ymax, _fill, fill, alpha, inherit);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_ErrorBar<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            Func<T2, TY2> ymin = null,
            Func<T2, TY2> ymax = null,
            IAestheticMapping<T2, string> _color = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            double radius = 5,
            PositionAdjustment position = PositionAdjustment.Identity,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new ErrorBar<T2, TX2, TY2>(source, x, y, ymin, ymax, _color, position, inherit)
                {
                    Line = new Line
                    {
                        Width = width,
                        Color = color,
                        Alpha = alpha,
                        LineType = lineType
                    },
                    Circle = new Circle
                    {
                        Fill = color,
                        Alpha = alpha,
                        Radius = radius
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_ErrorBar<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            Func<T2, TY2> ymin = null,
            Func<T2, TY2> ymax = null,
            IAestheticMapping<T2, string> _color = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            double radius = 5,
            PositionAdjustment position = PositionAdjustment.Identity,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_ErrorBar(source, x, y, ymin, ymax, _color, width, color, alpha, lineType, radius, position, inherit);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_ErrorBar<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            Func<T, TY> ymin = null,
            Func<T, TY> ymax = null,
            IAestheticMapping<T, string> _color = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            double radius = 5,
            PositionAdjustment position = PositionAdjustment.Identity,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            return Geom_ErrorBar(panel, panel.Data.Source, x, y, ymin, ymax, _color, width, color, alpha, lineType, radius, position, inherit);
        }

        public static Data<T, TX, TY> Geom_ErrorBar<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            Func<T, TY> ymin = null,
            Func<T, TY> ymax = null,
            IAestheticMapping<T, string> _color = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            double radius = 5,
            PositionAdjustment position = PositionAdjustment.Identity,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_ErrorBar(x, y, ymin, ymax, _color, width, color, alpha, lineType, radius, position, inherit);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_Text<T1, TX1, TY1, T2, TX2, TY2, TT>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            Func<T2, TT> text = null,
            IAestheticMapping<T2, string> _color = null,
            Size? size = null, Anchor anchor = Anchor.middle, string weight = "normal", string style = "normal", string color = "#23d0fc",
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new Text<T2, TX2, TY2, TT>(source, x, y, text, _color, inherit)
                {
                    Aesthetic = new Text
                    {
                        Size = size ?? new Size { Value = 1.0, Units = Units.em },
                        Anchor = anchor,
                        Weight = weight,
                        Style = style,
                        Color = color
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_Text<T1, TX1, TY1, T2, TX2, TY2, TT>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            Func<T2, TT> text = null,
            IAestheticMapping<T2, string> _color = null,
            Size? size = null, Anchor anchor = Anchor.middle, string weight = "normal", string style = "normal", string color = "#23d0fc",
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_Text(source, x, y, text, _color, size, anchor, weight, style, color, inherit);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_Text<T, TX, TY, TT>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            Func<T, TT> text = null,
            IAestheticMapping<T, string> _color = null,
            Size? size = null, Anchor anchor = Anchor.middle, string weight = "normal", string style = "normal", string color = "#23d0fc",
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            return Geom_Text(panel, panel.Data.Source, x, y, text, _color, size, anchor, weight, style, color, inherit);
        }

        public static Data<T, TX, TY> Geom_Text<T, TX, TY, TT>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            Func<T, TT> text = null,
            IAestheticMapping<T, string> _color = null,
            Size? size = null, Anchor anchor = Anchor.middle, string weight = "normal", string style = "normal", string color = "#23d0fc",
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_Text(x, y, text, _color, size, anchor, weight, style, color, inherit);

            return data;
        }

        public static Data<T1, TX1, TY>.PanelFactory Geom_VLine<T1, TX1, TY, T2, TX2>(
            this Data<T1, TX1, TY>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX1 : struct
            where TX2 : struct
            where TY : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new VLine<T2, TX2, TY>(source, x)
                {
                    Aesthetic = new Line
                    {
                        Width = width,
                        Color = color,
                        Alpha = alpha,
                        LineType = lineType
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY> Geom_VLine<T1, TX1, TY, T2, TX2>(
            this Data<T1, TX1, TY> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX1 : struct
            where TX2 : struct
            where TY : struct
        {
            data.Default_Panel().Geom_VLine(source, x, width, color, alpha, lineType);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_VLine<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX : struct
            where TY : struct
        {
            return Geom_VLine(panel, panel.Data.Source, x, width, color, alpha, lineType);
        }

        public static Data<T, TX, TY> Geom_VLine<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_VLine(x, width, color, alpha, lineType);

            return data;
        }

        public static Data<T1, TX, TY1>.PanelFactory Geom_HLine<T1, TX, TY1, T2, TY2>(
            this Data<T1, TX, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TY2> y = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new HLine<T2, TX, TY2>(source, y)
                {
                    Aesthetic = new Line
                    {
                        Width = width,
                        Color = color,
                        Alpha = alpha,
                        LineType = lineType
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX, TY1> Geom_HLine<T1, TX, TY1, T2, TY2>(
            this Data<T1, TX, TY1> data,
            Source<T2> source,
            Func<T2, TY2> y = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_HLine(source, y, width, color, alpha, lineType);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_HLine<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TY> y = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX : struct
            where TY : struct
        {
            return Geom_HLine(panel, panel.Data.Source, y, width, color, alpha, lineType);
        }

        public static Data<T, TX, TY> Geom_HLine<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TY> y = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_HLine(y, width, color, alpha, lineType);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_OHLC<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> open = null,
            Func<T2, TY2> high = null,
            Func<T2, TY2> low = null,
            Func<T2, TY2> close = null,
            Func<T2, MouseEventArgs, Task> onclick = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool vtrack = false, bool ylabel = false)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new OHLC<T2, TX2, TY2>(source, x, open, high, low, close, vtrack, ylabel)
                {
                    OnClick = onclick,
                    Aesthetic = new Line
                    {
                        Width = width,
                        Color = color,
                        Alpha = alpha,
                        LineType = lineType

                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_OHLC<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> open = null,
            Func<T2, TY2> high = null,
            Func<T2, TY2> low = null,
            Func<T2, TY2> close = null,
            Func<T2, MouseEventArgs, Task> onclick = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool vtrack = false, bool ylabel = false)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_OHLC(source, x, open, high, low, close, onclick, width, color, alpha, lineType, vtrack, ylabel);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_OHLC<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> open = null,
            Func<T, TY> high = null,
            Func<T, TY> low = null,
            Func<T, TY> close = null,
            Func<T, MouseEventArgs, Task> onclick = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool vtrack = false, bool ylabel = false)
            where TX : struct
            where TY : struct
        {
            return Geom_OHLC(panel, panel.Data.Source, x, open, high, low, close, onclick, width, color, alpha, lineType, vtrack, ylabel);
        }

        public static Data<T, TX, TY> Geom_OHLC<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> open = null,
            Func<T, TY> high = null,
            Func<T, TY> low = null,
            Func<T, TY> close = null,
            Func<T, MouseEventArgs, Task> onclick = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid,
            bool vtrack = false, bool ylabel = false)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_OHLC(x, open, high, low, close, onclick, width, color, alpha, lineType, vtrack, ylabel);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_Candlestick<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> open = null,
            Func<T2, TY2> high = null,
            Func<T2, TY2> low = null,
            Func<T2, TY2> close = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new Candlestick<T2, TX2, TY2>(source, x, open, high, low, close)
                {
                    Line = new Line
                    {
                        Width = width,
                        Color = color,
                        Alpha = alpha,
                        LineType = lineType
                    },
                    Rectangle = new Rectangle
                    {
                        Fill = color,
                        Alpha = alpha
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_Candlestick<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> open = null,
            Func<T2, TY2> high = null,
            Func<T2, TY2> low = null,
            Func<T2, TY2> close = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_Candlestick(source, x, open, high, low, close, width, color, alpha, lineType);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_Candlestick<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> open = null,
            Func<T, TY> high = null,
            Func<T, TY> low = null,
            Func<T, TY> close = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX : struct
            where TY : struct
        {
            return Geom_Candlestick(panel, panel.Data.Source, x, open, high, low, close, width, color, alpha, lineType);
        }

        public static Data<T, TX, TY> Geom_Candlestick<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> open = null,
            Func<T, TY> high = null,
            Func<T, TY> low = null,
            Func<T, TY> close = null,
            double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = LineType.Solid)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_Candlestick(x, open, high, low, close, width, color, alpha, lineType);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_Volume<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> volume = null,
            Func<T2, MouseEventArgs, Task> onclick = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool vtrack = false, bool ylabel = false)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new Volume<T2, TX2, TY2>(source, x, volume, vtrack, ylabel)
                {
                    OnClick = onclick,
                    Aesthetic = new Rectangle
                    {
                        Fill = fill,
                        Alpha = alpha
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_Volume<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> volume = null,
            Func<T2, MouseEventArgs, Task> onclick = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool vtrack = false, bool ylabel = false)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_Volume(source, x, volume, onclick, fill, alpha, vtrack, ylabel);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_Volume<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> volume = null,
            Func<T, MouseEventArgs, Task> onclick = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool vtrack = false, bool ylabel = false)
            where TX : struct
            where TY : struct
        {
            return Geom_Volume(panel, panel.Data.Source, x, volume, onclick, fill, alpha, vtrack, ylabel);
        }

        public static Data<T, TX, TY> Geom_Volume<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> volume = null,
            Func<T, MouseEventArgs, Task> onclick = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool vtrack = false, bool ylabel = false)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_Volume(x, volume, onclick, fill, alpha, vtrack, ylabel);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_Hex<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            Func<T2, TX2> dx = null,
            Func<T2, TY2> dy = null,
            IAestheticMapping<T2, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new Hex<T2, TX2, TY2>(source, x, y, dx, dy, _fill, inherit)
                {
                    Aesthetic = new Rectangle
                    {
                        Fill = fill,
                        Alpha = alpha
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            Func<T2, TX2> dx = null,
            Func<T2, TY2> dy = null,
            IAestheticMapping<T2, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_Hex(source, x, y, dx, dy, _fill, fill, alpha, inherit);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_Hex<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            Func<T, TX> dx = null,
            Func<T, TY> dy = null,
            IAestheticMapping<T, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            return Geom_Hex(panel, panel.Data.Source, x, y, dx, dy, _fill, fill, alpha, inherit);
        }

        public static Data<T, TX, TY> Geom_Hex<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            Func<T, TX> dx = null,
            Func<T, TY> dy = null,
            IAestheticMapping<T, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_Hex(x, y, dx, dy, _fill, fill, alpha, inherit);

            return data;
        }

        public static Data<T1, TX1, TY1>.PanelFactory Geom_RidgeLine<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1>.PanelFactory panel,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            Func<T2, double> height = null,
            IAestheticMapping<T2, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            panel.Add_Geom(() =>
            {
                var geom = new RidgeLine<T2, TX2, TY2>(source, x, y, height, _fill, inherit)
                {
                    Aesthetic = new Rectangle
                    {
                        Fill = fill,
                        Alpha = alpha
                    }
                };

                return geom;
            });

            return panel;
        }

        public static Data<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2, TX2, TY2>(
            this Data<T1, TX1, TY1> data,
            Source<T2> source,
            Func<T2, TX2> x = null,
            Func<T2, TY2> y = null,
            Func<T2, double> height = null,
            IAestheticMapping<T2, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX1 : struct
            where TX2 : struct
            where TY1 : struct
            where TY2 : struct
        {
            data.Default_Panel().Geom_RidgeLine(source, x, y, height, _fill, fill, alpha, inherit);

            return data;
        }

        public static Data<T, TX, TY>.PanelFactory Geom_RidgeLine<T, TX, TY>(
            this Data<T, TX, TY>.PanelFactory panel,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            Func<T, double> height = null,
            IAestheticMapping<T, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            return Geom_RidgeLine(panel, panel.Data.Source, x, y, height, _fill, fill, alpha, inherit);
        }

        public static Data<T, TX, TY> Geom_RidgeLine<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, TX> x = null,
            Func<T, TY> y = null,
            Func<T, double> height = null,
            IAestheticMapping<T, string> _fill = null,
            string fill = "#23d0fc", double alpha = 1.0,
            bool inherit = true)
            where TX : struct
            where TY : struct
        {
            data.Default_Panel().Geom_RidgeLine(x, y, height, _fill, fill, alpha, inherit);

            return data;
        }

        public static Data<T, TX, TY> Scale_Color_Discrete<T, TX, TY, TKey>(
            this Data<T, TX, TY> data,
            Func<T, TKey> selector,
            Palettes.Discrete<TKey, string> palette,
            bool guide = true,
            string name = null)
            where TX : struct
            where TY : struct
        {
            var scale = new ColorDiscrete<TKey>(palette);

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.Color = new Aesthetic<T, TKey, string>(selector, scale, guide, name);

            return data;
        }

        public static Data<T, TX, TY> Scale_Color_Discrete<T, TX, TY, TKey>(
            this Data<T, TX, TY> data,
            Func<T, TKey> selector,
            string[] palette,
            int direction = 1,
            bool guide = true,
            string name = null)
            where TX : struct
            where TY : struct
        {
            var scale = new ColorDiscrete<TKey>(palette, direction);

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.Color = new Aesthetic<T, TKey, string>(selector, scale, guide, name);

            return data;
        }

        public static Data<T, TX, TY> Scale_Color_Identity<T, TX, TY>(this Data<T, TX, TY> data, Func<T, string> selector)
            where TX : struct
            where TY : struct
        {
            var scale = new Scales.Identity<string>();

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.Color = new Aesthetic<T, string, string>(selector, scale, false, null);

            return data;
        }

        public static Data<T, TX, TY> Scale_Fill_Discrete<T, TX, TY, TKey>(
            this Data<T, TX, TY> data,
            Func<T, TKey> selector,
            Palettes.Discrete<TKey, string> palette,
            bool guide = true,
            string name = null)
            where TX : struct
            where TY : struct
        {
            var scale = new FillDiscrete<TKey>(palette);

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.Fill = new Aesthetic<T, TKey, string>(selector, scale, guide, name);

            return data;
        }

        public static Data<T, TX, TY> Scale_Fill_Discrete<T, TX, TY, TKey>(
            this Data<T, TX, TY> data,
            Func<T, TKey> selector,
            string[] palette,
            int direction = 1,
            bool guide = true,
            string name = null)
            where TX : struct
            where TY : struct
        {
            var scale = new FillDiscrete<TKey>(palette, direction);

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.Fill = new Aesthetic<T, TKey, string>(selector, scale, guide, name);

            return data;
        }

        public static Data<T, TX, TY> Scale_Fill_Continuous<T, TX, TY>(this Data<T, TX, TY> data,
            Func<T, double> selector,
            string[] palette,
            int m = 5,
            string format = "0.##",
            bool guide = true,
            string name = null)
            where TX : struct
            where TY : struct
        {
            var scale = new FillContinuous(palette, m, format);

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.Fill = new Aesthetic<T, double, string>(selector, scale, guide, name);

            return data;
        }

        public static Data<T, TX, TY> Scale_Fill_Identity<T, TX, TY>(this Data<T, TX, TY> data, Func<T, string> selector)
            where TX : struct
            where TY : struct
        {
            var scale = new Scales.Identity<string>();

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.Fill = new Aesthetic<T, string, string>(selector, scale, false, null);

            return data;
        }

        public static Data<T, TX, TY> Scale_Size_Continuous<T, TX, TY>(
            this Data<T, TX, TY> data,
            Func<T, double> selector,
            (double min, double max)? limits = null,
            (double min, double max)? range = null,
            bool oob = false,
            bool guide = true,
            string name = null,
            string format = "0.##")
            where TX : struct
            where TY : struct
        {
            var scale = new SizeContinuous(limits, range, oob, format);

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.Size = new Aesthetic<T, double, double>(selector, scale, guide, name);

            return data;
        }

        public static Data<T, TX, TY> Scale_Size_Identity<T, TX, TY>(this Data<T, TX, TY> data, Func<T, double> selector)
            where TX : struct
            where TY : struct
        {
            var scale = new Scales.Identity<double>();

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.Size = new Aesthetic<T, double, double>(selector, scale, false, null);

            return data;
        }
        
        public static Data<T, TX, TY> Scale_LineType_Discrete<T, TX, TY, TKey>(
            this Data<T, TX, TY> data,
            Func<T, TKey> selector,
            Palettes.Discrete<TKey, LineType> palette,
            bool guide = true,
            string name = null)
            where TX : struct
            where TY : struct
        {
            var scale = new LineTypeDiscrete<TKey>(palette);

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.LineType = new Aesthetic<T, TKey, LineType>(selector, scale, guide, name);

            return data;
        }

        public static Data<T, TX, TY> Scale_LineType_Discrete<T, TX, TY, TKey>(
            this Data<T, TX, TY> data,
            Func<T, TKey> selector,
            LineType[] palette = null,
            int direction = 1,
            bool guide = true,
            string name = null)
            where TX : struct
            where TY : struct
        {
            var scale = new LineTypeDiscrete<TKey>(palette, direction);

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.LineType = new Aesthetic<T, TKey, LineType>(selector, scale, guide, name);

            return data;
        }

        public static Data<T, TX, TY> Scale_LineType_Identity<T, TX, TY>(this Data<T, TX, TY> data, Func<T, LineType> selector)
            where TX : struct
            where TY : struct
        {
            var scale = new Scales.Identity<LineType>();

            data.Aesthetics.Scales.Add(scale);

            data.Aesthetics.LineType = new Aesthetic<T, LineType, LineType>(selector, scale, false, null);

            return data;
        }

        public static Data<T, TX, TY> Facet_Wrap<T, TX, TY, TKey>(this Data<T, TX, TY> data, Func<T, TKey> selector, bool freeX = false, bool freeY = false, int? nrows = null, int? ncolumns = null)
            where TX : struct
            where TY : struct
        {
            data.Faceting = new Faceting1D<T, TKey>(selector, freeX, freeY, nrows, ncolumns);

            return data;
        }

        public static Data<T, TX, TY> Facet_Grid<T, TX, TY, TRow, TColumn>(this Data<T, TX, TY> data, Func<T, TRow> row, Func<T, TColumn> column, bool freeX = false, bool freeY = false)
            where TX : struct
            where TY : struct
        {
            data.Faceting = new Faceting2D<T, TRow, TColumn>(row, column, freeX, freeY);

            return data;
        }

        public static Data<T, TX, TY> Theme<T, TX, TY>(
            this Data<T, TX, TY> data,
            Theme theme = null,
            bool dark = true,
            Position axisY = Left,
            Position legend = Right)
            where TX : struct
            where TY : struct
        {
            data.Theme = theme ?? GGNet.Theme.Default(dark, axisY, legend);

            return data;
        }
    }
}
