using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using NodaTime;

using GGNet.NaturalEarth;

namespace Demo.Pages
{
    public partial class Covid19 : ComponentBase
    {
        private async Task<List<TS>> GetData()
        {
            var csv = await Http.GetStringAsync("https://raw.githubusercontent.com/owid/covid-19-data/master/input/ecdc/releases/latest.csv");

            static TS.DoubleStat ConfirmedDouble(TS.Point[] points, TS.Point last)
            {
                TS.DoubleStat confirmedDouble = null;

                for (var i = 1; i < points.Length - 1; i++)
                {
                    var point = points[points.Length - 1 - i];

                    if (point.ConfirmedCumulative == 0)
                    {
                        break;
                    }

                    if (last.ConfirmedCumulative >= 2 * point.ConfirmedCumulative)
                    {
                        confirmedDouble = new TS.DoubleStat
                        {
                            Date = point.Date,
                            Days = i,
                            Rate = last.ConfirmedCumulative / point.ConfirmedCumulative
                        };

                        break;
                    }
                }

                return confirmedDouble;
            }

            static TS.DoubleStat DeathsDouble(TS.Point[] points, TS.Point last)
            {
                TS.DoubleStat deathsDouble = null;

                for (var i = 1; i < points.Length - 1; i++)
                {
                    var point = points[points.Length - 1 - i];

                    if (point.DeathsCumulative == 0)
                    {
                        break;
                    }

                    if (last.DeathsCumulative >= 2 * point.DeathsCumulative)
                    {
                        deathsDouble = new TS.DoubleStat
                        {
                            Date = point.Date,
                            Days = i,
                            Rate = last.DeathsCumulative / point.DeathsCumulative
                        };

                        break;
                    }
                }

                return deathsDouble;
            }

            var data = csv
                .Split('\n')
                .Skip(1)
                .Select(line =>
                {
                    var fields = line.Split(',');

                    if (fields.Count() != 10)
                    {
                        return default;
                    }

                    var a2 = fields[7];

                    var year = int.Parse(fields[3]);
                    var month = int.Parse(fields[2]);
                    var day = int.Parse(fields[1]);

                    var date = new LocalDate(year, month, day);

                    var cases = double.Parse(fields[4]);
                    var deaths = double.Parse(fields[5]);

                    long.TryParse(fields[9], out var population);

                    return (a2, date, cases, deaths, population);
                })
                .Where(o => !string.IsNullOrEmpty(o.a2))
                .GroupBy(o => o.a2)
                .Select(g =>
                {
                    var points = g
                        .OrderBy(o => o.date)
                        .Select(o => new TS.Point
                        {
                            Date = o.date,
                            ConfirmedDelta = o.cases,
                            DeathsDelta = o.deaths
                        })
                        .ToArray();

                    var confirmedCumulative = 0.0;
                    var deathsCumulative = 0.0;

                    for (var i = 0; i < points.Length; i++)
                    {
                        var point = points[i];

                        confirmedCumulative += point.ConfirmedDelta;
                        deathsCumulative += point.DeathsDelta;

                        point.ConfirmedCumulative = confirmedCumulative;
                        point.DeathsCumulative = deathsCumulative;
                    }

                    var last = points[^1];

                    var ts = new TS
                    {
                        Country = Scale110.Countries.SingleOrDefault(o => o.A2 == g.Key),
                        Points = points,
                        Population = g.First().population,
                        ConfirmedDelta = last.ConfirmedDelta,
                        ConfirmedCumulative = last.ConfirmedCumulative,
                        DeathsDelta = last.DeathsDelta,
                        DeathsCumulative = last.DeathsCumulative
                    };

                    ts.ConfirmedDouble = ConfirmedDouble(ts.Points, last);
                    ts.DeathsDouble = DeathsDouble(ts.Points, last);

                    return ts;
                })
                .Where(o => o.Country != null && o.Country.Capital != null)
                .ToList();

            var points = new SortedDictionary<LocalDate, TS.Point>();

            long population = 0;
            foreach (var ts in data)
            {
                population += ts.Population;

                for (var i = 0; i < ts.Points.Length; i++)
                {
                    var point = ts.Points[i];

                    if (!points.TryGetValue(point.Date, out var p))
                    {
                        p = new TS.Point { Date = point.Date };

                        points[point.Date] = p;
                    }

                    p.ConfirmedCumulative += point.ConfirmedCumulative;
                    p.ConfirmedDelta += point.ConfirmedDelta;
                    p.DeathsCumulative += point.DeathsCumulative;
                    p.DeathsDelta += point.DeathsDelta;
                }
            }

            var tspoints = points.Values.ToArray();
            var last = tspoints[^1];

            data.Insert(0, new TS
            {
                Country = new Country { Name = "World", A2 = "*" },
                Points = tspoints,
                Population = population,
                ConfirmedDelta = last.ConfirmedDelta,
                ConfirmedCumulative = last.ConfirmedCumulative,
                DeathsDelta = last.DeathsDelta,
                DeathsCumulative = last.DeathsCumulative,
                ConfirmedDouble = ConfirmedDouble(tspoints, last),
                DeathsDouble = DeathsDouble(tspoints, last)
            });

            return data;
        }

        private List<StatPoint> GetStatPoints(TS.Point[] points)
        {
            var stats = new List<StatPoint>();

            for (var i = 0; i < points.Length; i++)
            {
                var point = points[i];

                stats.Add(new StatPoint
                {
                    Date = point.Date,
                    Stat = Stat.Deaths,
                    Delta = point.DeathsDelta,
                    Cumulative = point.DeathsCumulative
                });

                stats.Add(new StatPoint
                {
                    Date = point.Date,
                    Stat = Stat.Confirmed,
                    Delta = point.ConfirmedDelta,
                    Cumulative = point.ConfirmedCumulative,
                });
            }

            return stats;
        }
    }

    public class TS
    {
        public class Point
        {
            public LocalDate Date { get; set; }

            public double ConfirmedDelta { get; set; }

            public double ConfirmedCumulative { get; set; }

            public double DeathsDelta { get; set; }

            public double DeathsCumulative { get; set; }
        }

        public class DoubleStat
        {
            public LocalDate Date { get; set; }

            public int Days { get; set; }

            public double Rate { get; set; }
        }

        public Country Country { get; set; }

        public string Name => Country.Name;

        public Point[] Points { get; set; }

        public long Population { get; set; }

        public double ConfirmedDelta { get; set; }

        public double ConfirmedCumulative { get; set; }

        public DoubleStat ConfirmedDouble { get; set; }

        public int ConfirmedDoubleDays => ConfirmedDouble?.Days ?? 0;

        public double DeathsDelta { get; set; }

        public double DeathsCumulative { get; set; }

        public DoubleStat DeathsDouble { get; set; }

        public int DeathsDoubleDays => DeathsDouble?.Days ?? 0;

        public double CFR => DeathsCumulative / ConfirmedCumulative;
    }

    public enum Stat
    {
        Confirmed,
        Deaths
    }

    public class StatPoint
    {
        public LocalDate Date { get; set; }

        public Stat Stat { get; set; }

        public double Delta { get; set; }

        public double Cumulative { get; set; }
    }
}
