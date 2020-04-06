using MessagePack;

using GGNet.Geospacial;

namespace GGNet.NaturalEarth
{
    [MessagePackObject]
    public class City
    {
        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public Point Point { get; set; }
    }

    [MessagePackObject]
    public class Country
    {
        [Key(0)]
        public string A2 { get; set; }

        [Key(1)]
        public string A3 { get; set; }

        [Key(2)]
        public string Name { get; set; }

        [Key(3)]
        public string Continent { get; set; }

        [Key(4)]
        public Polygon[] Polygons { get; set; }

        [Key(5)]
        public City Capital { get; set; }

        [Key(6)]
        public Point Centroid { get; set; }

        public override string ToString() => $"{A2} - {Name}";
    }

    [MessagePackObject]
    public class Lake
    {
        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public Polygon[] Polygons { get; set; }
    }
}
