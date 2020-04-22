using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using NodaTime;
using NodaTime.Text;

using GGNet.NaturalEarth;

namespace Demo.Pages
{
    public partial class Covid19 : ComponentBase
    {

        private static readonly LocalDatePattern pattern = LocalDatePattern.CreateWithInvariantCulture("yyyy-MM-dd");

        private async Task<List<TS>> GetData()
        {
            var pop = (await Http.GetStringAsync("https://raw.githubusercontent.com/owid/covid-19-data/master/public/data/ecdc/locations.csv"))
                            .Split('\n')
                            .Skip(1)
                            .Select(line =>
                            {
                                var fields = line.Split(',');
                                var n = fields.Length;

                                if (n < 5)
                                {
                                    return default;
                                }

                                long.TryParse(fields[n - 1], out var population);

                                return (location: fields[n - 4], population);
                            })
                            .Where(o => !string.IsNullOrEmpty(o.location))
                            .ToList();

            //iso_code,location,date,total_cases,new_cases,total_deaths,new_deaths,total_cases_per_million,new_cases_per_million,total_deaths_per_million,new_deaths_per_million,total_tests,new_tests,total_tests_per_thousand,new_tests_per_thousand,tests_units
            var csv = await Http.GetStringAsync("https://raw.githubusercontent.com/owid/covid-19-data/master/public/data/owid-covid-data.csv").ConfigureAwait(false);

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

                    if (fields.Count() <= 7)
                    {
                        return default;
                    }

                    var iso = fields[0];
                    var location = fields[1];

                    var date = pattern.Parse(fields[2]).Value;

                    var cases = double.Parse(fields[4]);
                    var deaths = double.Parse(fields[6]);

                    return (iso, location, date, cases, deaths);
                })
                .Where(o => !string.IsNullOrEmpty(o.iso))
                .GroupBy(o => (o.iso, o.location))
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
                        Country = Scale110.Countries.SingleOrDefault(o => o.A3 == g.Key.iso),
                        Points = points,
                        Population = pop.SingleOrDefault(o => o.location == g.Key.location).population,
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

            return data.OrderByDescending(o => o.ConfirmedCumulative).ToList();
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

        private async Task<List<(string a3, double age60_, double age40_59, double age_20_39)>> GetAgeData()
        {
            var csv = await Http.GetStringAsync(Navigation.ToAbsoluteUri("data/Age.csv"));

            var data = csv
                .Split('\n')
                .Skip(1)
                .Select(line =>
                {
                    var fields = line.Split(',');

                    var a3 = fields[0];

                    var age60_ = double.Parse(fields[1]);
                    var age40_59 = double.Parse(fields[2]);
                    var age20_39 = double.Parse(fields[3]);

                    return (a3, age60_, age40_59, age20_39);
                })
                .ToList();

            return data;
        }

        private async Task<List<(string a3, double obesity)>> GetObesityData()
        {
            var csv = await Http.GetStringAsync(Navigation.ToAbsoluteUri("data/Obesity.csv"));

            var data = csv
                .Split('\n')
                .Skip(1)
                .Select(line =>
                {
                    var fields = line.Split(',');

                    var a3 = fields[0];
                    var obesity = double.Parse(fields[1]);

                    return (a3, obesity);
                })
                .ToList();

            return data;
        }

        private async Task<List<(string a3, double probability)>> GetProbabilityData()
        {
            var csv = await Http.GetStringAsync(Navigation.ToAbsoluteUri("data/Probability.csv"));

            var data = csv
                .Split('\n')
                .Skip(1)
                .Select(line =>
                {
                    var fields = line.Split(',');

                    var a3 = fields[0];
                    var probability = double.Parse(fields[1]);

                    return (a3, probability);
                })
                .ToList();

            return data;
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
