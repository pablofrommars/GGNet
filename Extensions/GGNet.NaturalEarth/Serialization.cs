using System;

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

using GGNet.Geospacial;

namespace GGNet.NaturalEarth
{
    public static class Serialization
    {
        private readonly static MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithResolver(Resolver.Instance);

        public static T Load<T>(string data) => MessagePackSerializer.Deserialize<T>(Convert.FromBase64String(data), options);

        public class PointFormatter : IMessagePackFormatter<Point>
        {
            public static readonly PointFormatter Instance = new PointFormatter();

            public Point Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                if (reader.IsNil)
                {
                    return null;
                }

                reader.ReadArrayHeader();

                return new Point
                {
                    Longitude = reader.ReadDouble(),
                    Latitude = reader.ReadDouble()
                };
            }

            public void Serialize(ref MessagePackWriter writer, Point value, MessagePackSerializerOptions options)
                => throw new NotSupportedException();
        }

        public class PolygonFormatter : IMessagePackFormatter<Polygon>
        {
            public static readonly PolygonFormatter Instance = new PolygonFormatter();

            public Polygon Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                reader.ReadArrayHeader();

                var hole = reader.ReadBoolean();

                var nx = reader.ReadArrayHeader();
                var x = new double[nx];

                for (var j = 0; j < nx; j++)
                {
                    x[j] = reader.ReadDouble();
                }

                var ny = reader.ReadArrayHeader();
                var y = new double[ny];

                for (var j = 0; j < ny; j++)
                {
                    y[j] = reader.ReadDouble();
                }

                return new Polygon
                {
                    Hole = hole,
                    Longitude = x,
                    Latitude = y
                };
            }

            public void Serialize(ref MessagePackWriter writer, Polygon value, MessagePackSerializerOptions options)
                => throw new NotSupportedException();
        }

        public class Resolver : IFormatterResolver
        {
            public static readonly IFormatterResolver Instance = new Resolver();

            private Resolver() { }

            public IMessagePackFormatter<T> GetFormatter<T>() => Cache<T>.Formatter;

            private static class Cache<T>
            {
                public static IMessagePackFormatter<T> Formatter;

                static Cache()
                {
                    var type = typeof(T);
                    if (type == typeof(Point))
                    {
                        Formatter = PointFormatter.Instance as IMessagePackFormatter<T>;
                    }
                    else if (type == typeof(Polygon))
                    {
                        Formatter = PolygonFormatter.Instance as IMessagePackFormatter<T>;
                    }
                    else
                    {
                        Formatter = StandardResolver.Instance.GetFormatter<T>();
                    }
                }
            }
        }
    }
}
