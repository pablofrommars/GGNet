using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using GGNet.Datasets;

using static System.Math;

namespace GGNet.Static.Demo
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var theme = Theme.Default();

			theme.Plot.SubTitle.Color = "#E2E8F0";
			theme.Plot.Background.Fill = "#334155";
			theme.Panel.Background.Fill = "#334155";
			theme.Axis.Text.X.Color = "#F1F5F9";
			theme.Axis.Title.X.Color = "#F1F5F9";
			theme.Axis.Text.Y.Color = "#F1F5F9";
			theme.Axis.Text.Y.Size = new Size(10, Units.px);
			theme.Axis.Title.Y.Color = "#F1F5F9";
			theme.Panel.Grid.Major.X.Fill = "#CBD5E1";
			theme.Panel.Grid.Minor.X.Fill = "#CBD5E1";
			theme.Legend.Title.Color = "#F1F5F9";
			theme.Legend.Labels.Color = "#F1F5F9";

			var a0 = 2.0;
			var v0 = 0.0;
			var vref = 10.0;
			var jmax = 2.0;
			var amax = 3.0;

			var trajectory = new AccelerationSCurveTrajectory(a0, v0, vref, jmax, amax);

			var plot = Plot.New(Enumerable.Range(0, 1200).Select(o => o * trajectory.T / 1000.0))
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => trajectory.Jerk(t), color: "#FBBF24", width: 2)
					.YLab("J (m/s^3)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => trajectory.Acceleration(t), color: "#FB7185", width: 2)
					.YLab("A (m/s^2)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => trajectory.Velocity(t), color: "#60A5FA", width: 2.0)
					.YLab("V (m/s)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => trajectory.Position(t), color: "#34D399", width: 2.0)
					.Geom_VLine(trajectory.Breaks.Select(o => o.t), t => t, t => $"T = {t:N2}", color: "#FFFFFF", lineType: LineType.Dashed)
					.YLab("Position (m)"), height: 0.4)
				.XLab("Time (s)")
				.Title($"Speed from {v0} to {vref}m/s")
				.SubTitle($"JMax:{jmax} AMax:{amax} A0:{a0}")
				.Theme(theme);

			await plot.Save("trajectory.svg");
		}

		static async Task MainA(string[] args)
		{
			var theme = Theme.Default();

			theme.Plot.SubTitle.Color = "#E2E8F0";
			theme.Plot.Background.Fill = "#334155";
			theme.Panel.Background.Fill = "#334155";
			theme.Axis.Text.X.Color = "#F1F5F9";
			theme.Axis.Title.X.Color = "#F1F5F9";
			theme.Axis.Text.Y.Color = "#F1F5F9";
			theme.Axis.Text.Y.Size = new Size(10, Units.px);
			theme.Axis.Title.Y.Color = "#F1F5F9";
			theme.Panel.Grid.Major.X.Fill = "#CBD5E1";
			theme.Panel.Grid.Minor.X.Fill = "#CBD5E1";
			theme.Legend.Title.Color = "#F1F5F9";
			theme.Legend.Labels.Color = "#F1F5F9";

			//var AMax = 3.0;
			//var AMax = 2.8284271247461903;
			var AMax = 2.0;
			var JMax = 2.0;

			var a0 = 2;
			var v0 = 5.0;
			var vref = 8.0;

			var condition = vref - v0 + (0.5 * a0 * a0 - AMax * AMax) / JMax;

			var t1 = (AMax - a0) / JMax;
			var v1 = JMax / 2.0 * t1 * t1 + a0 * t1 + v0;

			var t3 = AMax / JMax;

			var v2 = vref - AMax * AMax / (2.0 * JMax);

			var t2 = (v2 - v1) / AMax;

			var T12 = t1 + t2;
			var T123 = t1 + t2 + t3;

			var s1 = JMax / 6.0 * t1 * t1 * t1 + a0 / 2.0 * t1 * t1 + v0 * t1;
			var s2 = AMax / 2.0 * t2 * t2 + v1 * t2 + s1;

			//

			double Jerk(double t)
			{
				if (t < t1)
				{
					return JMax;
				}
				else if (t < T12)
				{
					return 0.0;
				}
				else
				{
					return -JMax;
				}
			}

			double Acceleration(double t)
			{
				if (t < t1)
				{
					return JMax * t + a0;
				}
				else if (t < T12)
				{
					return AMax;
				}
				else
				{
					return -JMax * (t - T12) + AMax;
				}
			}

			double Velocity(double t)
			{
				if (t < t1)
				{
					return JMax / 2.0 * t * t + a0 * t + v0;
				}
				else if (t < T12)
				{
					return AMax * (t - t1) + v1;
				}
				else
				{
					return -JMax / 2.0 * (t - T12) * (t - T12) + AMax * (t - T12) + v2;
				}
			}

			double Position(double t)
			{
				if (t < t1)
				{
					return JMax / 6.0 * t * t * t + a0 / 2.0 * t * t + v0 * t;
				}
				else if (t < T12)
				{
					return AMax / 2.0 * (t - t1) * (t - t1) + v1 * (t - t1) + s1;
				}
				else
				{
					return -JMax / 6.0 * (t - T12) * (t - T12) * (t - T12) + AMax / 2.0 * (t - T12) * (t - T12) + v2 * (t - T12) + s2;
				}
			}

			var plot = Plot.New(Enumerable.Range(0, 1000).Select(o => o * T123 / 1000.0))
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Jerk(t), color: "#FBBF24", width: 2)
					.YLab("J (m/s^3)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Acceleration(t), color: "#FB7185", width: 2)
					.YLab("A (m/s^2)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Velocity(t), color: "#60A5FA", width: 2.0)
					.YLab("V (m/s)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Position(t), color: "#34D399", width: 2.0)
					.Geom_VLine(new[] { t1, T12, T123 }, t => t, t => $"T = {t:N2}", color: "#FFFFFF", lineType: LineType.Dashed)
					.YLab("Position (m)"), height: 0.4)
				.XLab("Time (s)")
				.Title($"Speed from {v0} to {vref}m/s")
				.SubTitle($"JMax:{JMax} AMax:{AMax} A0:{a0}")
				.Theme(theme);

			await plot.Save("trajectory.svg");
		}

		static async Task MainD(string[] args)
		{
			var theme = Theme.Default();

			theme.Plot.SubTitle.Color = "#E2E8F0";
			theme.Plot.Background.Fill = "#334155";
			theme.Panel.Background.Fill = "#334155";
			theme.Axis.Text.X.Color = "#F1F5F9";
			theme.Axis.Title.X.Color = "#F1F5F9";
			theme.Axis.Text.Y.Color = "#F1F5F9";
			theme.Axis.Text.Y.Size = new Size(10, Units.px);
			theme.Axis.Title.Y.Color = "#F1F5F9";
			theme.Panel.Grid.Major.X.Fill = "#CBD5E1";
			theme.Panel.Grid.Minor.X.Fill = "#CBD5E1";
			theme.Legend.Title.Color = "#F1F5F9";
			theme.Legend.Labels.Color = "#F1F5F9";

			var AMax = 3.0;
			var JMax = 2.0;

			var a0 = -2.0;
			var v0 = 8.0;
			var vref = 0.0;

			var t1 = (AMax + a0) / JMax;
			var v1 = -JMax / 2.0 * t1 * t1 + a0 * t1 + v0;

			var t3 = AMax / JMax;

			var v2 = vref + AMax * AMax / (2.0 * JMax);

			var t2 = (v1 - v2) / AMax;

			var T12 = t1 + t2;
			var T123 = t1 + t2 + t3;

			var s1 = -JMax / 6.0 * t1 * t1 * t1 + a0 / 2.0 * t1 * t1 + v0 * t1;
			var s2 = -AMax / 2.0 * t2 * t2 + v1 * t2 + s1;

			double Jerk(double t)
			{
				if (t < t1)
				{
					return -JMax;
				}
				else if (t < T12)
				{
					return 0.0;
				}
				else
				{
					return JMax;
				}
			}

			double Acceleration(double t)
			{
				if (t < t1)
				{
					return -JMax * t + a0;
				}
				else if (t < T12)
				{
					return -AMax;
				}
				else
				{
					return JMax * (t - T12) - AMax;
				}
			}

			double Velocity(double t)
			{
				if (t < t1)
				{
					return -JMax / 2.0 * t * t + a0 * t + v0;
				}
				else if (t < T12)
				{
					return v1 - AMax * (t - t1);
				}
				else
				{
					return JMax / 2.0 * (t - T12) * (t - T12) - AMax * (t - T12) + v2;
				}
			}

			double Position(double t)
			{
				if (t < t1)
				{
					return -JMax / 6.0 * t * t * t + a0 / 2.0 * t * t + v0 * t;
				}
				else if (t < T12)
				{
					return -AMax / 2.0 * (t - t1) * (t - t1) + v1 * (t - t1) + s1;
				}
				else
				{
					return JMax / 6.0 * (t - T12) * (t - T12) * (t - T12) - AMax / 2.0 * (t - T12) * (t - T12) + v2 * (t - T12) + s2;
				}
			}

			var plot = Plot.New(Enumerable.Range(0, 1000).Select(o => o * T123 / 1000.0))
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Jerk(t), color: "#FBBF24", width: 2)
					.YLab("J (m/s^3)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Acceleration(t), color: "#FB7185", width: 2)
					.YLab("A (m/s^2)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Velocity(t), color: "#60A5FA", width: 2.0)
					.YLab("V (m/s)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Position(t), color: "#34D399", width: 2.0)
					.Geom_VLine(new[] { t1, T12, T123 }, t => t, t => $"T = {t:N2}", color: "#FFFFFF", lineType: LineType.Dashed)
					.YLab("Position (m)"), height: 0.4)
				.XLab("Time (s)")
				.Title($"Speed from {v0} to {vref}m/s")
				.SubTitle($"JMax:{JMax} AMax:{AMax} A0:{a0}")
				.Theme(theme);

			await plot.Save("trajectory.svg");
		}

		static async Task MainAST(string[] args)
		{
			var theme = Theme.Default();

			theme.Plot.SubTitle.Color = "#E2E8F0";
			theme.Plot.Background.Fill = "#334155";
			theme.Panel.Background.Fill = "#334155";
			theme.Axis.Text.X.Color = "#F1F5F9";
			theme.Axis.Title.X.Color = "#F1F5F9";
			theme.Axis.Text.Y.Color = "#F1F5F9";
			theme.Axis.Text.Y.Size = new Size(10, Units.px);
			theme.Axis.Title.Y.Color = "#F1F5F9";
			theme.Panel.Grid.Major.X.Fill = "#CBD5E1";
			theme.Panel.Grid.Minor.X.Fill = "#CBD5E1";
			theme.Legend.Title.Color = "#F1F5F9";
			theme.Legend.Labels.Color = "#F1F5F9";

			var AMax = 3.0;
			var JMax = 2.0;

			var a0 = 2.0;
			var v0 = 5.0;
			var vref = 8.0;

			// v0 + a0 * Tt + JMax / 2 + Tt^2 = vref - JMax / 2 * (T -Tt)^2
			// a0 + JMax * Tt = JMax * (T - Tt)

			var deltaV = vref - v0;
			var condition = 4.0 * (a0 * a0 / 2.0 + JMax * deltaV); // >= 0 for solution

			var Tt = (-a0 + Sqrt(a0 * a0 / 2.0 + JMax * deltaV)) / JMax;
			var T = a0 / JMax + 2.0 * Tt;

			var a = JMax * Tt + a0;
			var v = JMax / 2.0 * Tt * Tt + a0 * Tt + v0;
			var s = JMax / 6.0 * Tt * Tt * Tt + a0 / 2.0 * Tt * Tt + v0 * Tt;

			double Jerk(double t) =>
				t < Tt
				? JMax
				: -JMax;

			double Acceleration(double t) =>
				t < Tt
				? JMax * t + a0
				: -JMax * (t - Tt) + a;

			double Velocity(double t) =>
				t < Tt
				? JMax / 2.0 * t * t + a0 * t + v0
				: -JMax / 2.0 * (t - Tt) * (t - Tt) + a * (t - Tt) + v;

			double Position(double t) =>
				t < Tt
				? JMax / 6.0 * t * t * t + a0 / 2.0 * t * t + v0 * t
				: -JMax / 6.0 * (t - Tt) * (t - Tt) * (t - Tt) + a / 2.0 * (t - Tt) * (t - Tt) + v * (t - Tt) + s;

			var plot = Plot.New(Enumerable.Range(0, 1000).Select(o => o * T / 1000.0))
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Jerk(t), color: "#FBBF24", width: 2)
					.YLab("J (m/s^3)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Acceleration(t), color: "#FB7185", width: 2)
					.YLab("A (m/s^2)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Velocity(t), color: "#60A5FA", width: 2.0)
					.YLab("V (m/s)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Position(t), color: "#34D399", width: 2.0)
					.Geom_VLine(new[] { Tt, T }, t => t, t => $"T = {t:N2}", color: "#FFFFFF", lineType: LineType.Dashed)
					.YLab("Position (m)"), height: 0.4)
				.XLab("Time (s)")
				.Title($"Speed from {v0} to {vref}m/s")
				.SubTitle($"JMax:{JMax} AMax:{AMax} A0:{a0}")
				.Theme(theme);

			await plot.Save("trajectory.svg");
		}

		static async Task MainDST(string[] args)
		{
			var theme = Theme.Default();

			theme.Plot.SubTitle.Color = "#E2E8F0";
			theme.Plot.Background.Fill = "#334155";
			theme.Panel.Background.Fill = "#334155";
			theme.Axis.Text.X.Color = "#F1F5F9";
			theme.Axis.Title.X.Color = "#F1F5F9";
			theme.Axis.Text.Y.Color = "#F1F5F9";
			theme.Axis.Text.Y.Size = new Size(10, Units.px);
			theme.Axis.Title.Y.Color = "#F1F5F9";
			theme.Panel.Grid.Major.X.Fill = "#CBD5E1";
			theme.Panel.Grid.Minor.X.Fill = "#CBD5E1";
			theme.Legend.Title.Color = "#F1F5F9";
			theme.Legend.Labels.Color = "#F1F5F9";

			var AMax = 3.0;
			var JMax = 2.0;

			var a0 = -2.0;
			var v0 = 8.0;
			var vref = 5.0;

			// v0 + a0 * Tt - JMax / 2 + Tt^2 = vref + JMax / 2 * (T -Tt)^2
			// a0 - JMax * Tt = -JMax * (T - Tt)

			var deltaV = vref - v0;
			var condition = 4.0 * (a0 * a0 / 2.0 - JMax * deltaV); // >= 0 for solution

			var Tt = (a0 + Sqrt(a0 * a0 / 2.0 - JMax * deltaV)) / JMax;
			var T = 2.0 * Tt - a0 / JMax;

			var a = a0 - JMax * Tt;
			var v = -JMax / 2.0 * Tt * Tt + a0 * Tt + v0;
			var s = -JMax / 6.0 * Tt * Tt * Tt + a0 / 2.0 * Tt * Tt + v0 * Tt;

			double Jerk(double t) =>
				t < Tt
				? -JMax
				: JMax;

			double Acceleration(double t) =>
				t < Tt
				? -JMax * t + a0
				: JMax * (t - Tt) + a;

			double Velocity(double t) =>
				t < Tt
				? -JMax / 2.0 * t * t + a0 * t + v0
				: JMax / 2.0 * (t - Tt) * (t - Tt) + a * (t - Tt) + v;

			double Position(double t) =>
				t < Tt
				? -JMax / 6.0 * t * t * t + a0 / 2.0 * t * t + v0 * t
				: JMax / 6.0 * (t - Tt) * (t - Tt) * (t - Tt) + a / 2.0 * (t - Tt) * (t - Tt) + v * (t - Tt) + s;

			var plot = Plot.New(Enumerable.Range(0, 1000).Select(o => o * T / 1000.0))
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Jerk(t), color: "#FBBF24", width: 2)
					.YLab("J (m/s^3)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Acceleration(t), color: "#FB7185", width: 2)
					.YLab("A (m/s^2)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Velocity(t), color: "#60A5FA", width: 2.0)
					.YLab("V (m/s)"), height: 0.2)
				.Panel(pf => pf
					.Geom_Line(x: t => t, y: t => Position(t), color: "#34D399", width: 2.0)
					.Geom_VLine(new[] { Tt, T }, t => t, t => $"T = {t:N2}", color: "#FFFFFF", lineType: LineType.Dashed)
					.YLab("Position (m)"), height: 0.4)
				.XLab("Time (s)")
				.Title($"Speed from {v0} to {vref}m/s")
				.SubTitle($"JMax:{JMax} AMax:{AMax} A0:{a0}")
				.Theme(theme);

			await plot.Save("trajectory.svg");
		}

		static async Task Main2(string[] args)
		{
			var data = (await JsonSerializer.DeserializeAsync<List<Point>>(File.OpenRead(@"data.json")))
				.Skip(20)
				.ToList();

			var values1 = new double[500];
			var avg1 = new GGNet.Source<Point>();

			var values2 = new double[50];
			var avg2 = new GGNet.Source<Point>();

			var values3 = new double[250];
			var avg3 = new GGNet.Source<Point>();
			var std3 = new GGNet.Source<Point>();

			for (var i = 0; i < data.Count; i++)
			{
				values1[i % values1.Length] = data[i].Current;
				avg1.Add(new Point(data[i].Count, values1.Average()));
				values2[i % values2.Length] = data[i].Current;
				avg2.Add(new Point(data[i].Count, values2.Average()));

				values3[i % values3.Length] = data[i].Current;
				var avg = values3.Average();
				avg3.Add(new Point(data[i].Count, avg));
				var std = values3.Sum(o => (o - avg) * (o - avg)) / (values3.Length - 1);
				std3.Add(new Point(data[i].Count, std));
			}

			var sum = 0.0;
			for (var i = 500; i < std3.Count; i++)
			{
				sum += std3[i].Current;
			}

			sum /= (std3.Count - 500);

			Console.WriteLine(3.3 * Math.Sqrt(2.0 * sum / 250.0));

			var plot = Plot.New(data.Skip(10), x: o => -o.Count, y: o => o.Current)
				.Geom_Point(size: 1)
				.Geom_Line(avg1, x: o => -o.Count, y: o => o.Current, color: "red")
				.Geom_Line(avg2, x: o => -o.Count, y: o => o.Current, color: "yellow")
				.Geom_Line(avg3, x: o => -o.Count, y: o => o.Current, color: "purple")
				.Geom_Line(std3, x: o => -o.Count, y: o => o.Current, color: "purple")
				.Scale_Y_Continuous(limits: (null, 0.5))
				.Title("Ground Test")
				.XLab("Encoder Count")
				.YLab("Current")
				.Theme(dark: false);

			/*
                var plot = Plot.New(Tip.Load(), x: o => o.Day, y: o => o.Avg)
				.Geom_ErrorBar(ymin: o => o.Lower, ymax: o => o.Upper, position: PositionAdjustment.Dodge)
				.Scale_Color_Discrete(o => o.Sex, new[] { "#69b3a2", "#404080" })
				.YLab("Tip (%)")
				.Theme(dark: false);
            */

			//Save to File
			await plot.Save("tip.svg");

			//Render as string
			var svg = await plot.AsString();

		}

		public sealed class AccelerationSCurveTrajectory
		{
			private readonly double a0;
			private readonly double v0;
			private readonly double vref;
			private readonly double jmax;
			private readonly double amax;

			private readonly double t1;
			private readonly double s1;
			private readonly double v1;

			private readonly double t2;
			private readonly double s2;
			private readonly double v2;

			private readonly double t3;
			private readonly double s3;

			private readonly double T12;
			private readonly double S12;

			public AccelerationSCurveTrajectory(double a0, double v0, double vref, double jmax = 2.0, double amax = 5.0)
			{
				this.a0 = a0;
				this.v0 = v0;
				this.vref = vref;
				this.jmax = jmax;
				this.amax = amax;

				t1 = (amax - a0) / jmax;
				v1 = jmax / 2.0 * t1 * t1 + a0 * t1 + v0;

				t3 = amax / jmax;

				v2 = vref - amax * amax / (2.0 * jmax);

				t2 = (v2 - v1) / amax;

				T12 = t1 + t2;

				T = t1 + t2 + t3;

				s1 = jmax / 6.0 * t1 * t1 * t1 + a0 / 2.0 * t1 * t1 + v0 * t1;
				s2 = amax / 2.0 * t2 * t2 + v1 * t2;

				S12 = s1 + s2;

				s3 = -jmax / 6.0 * t3 * t3 * t3 + amax / 2.0 * t3 * t3 + v2 * t3;

				S = S12 + s3;

				Breaks = new[]
				{
					(t1, s1, v1),
					(T12, S12, v2),
					(T, S, vref)
				};
			}

			public double T { get; }

			public double S { get; }

			public (double t, double s, double v)[] Breaks { get; }

			private double JerkConcavePhase(double t) => jmax;
			private static double JerkLinearPhase(double t) => 0.0;
			private double JerkConvexPhase(double t) => -jmax;
			private static double JerkPostPhase(double t) => 0.0;

			public double Jerk(double t)
			{
				if (t < t1)
				{
					return JerkConcavePhase(t);
				}
				else if (t < T12)
				{
					return JerkLinearPhase(t);
				}
				else if (t < T)
				{
					return JerkConvexPhase(t);
				}
				else
				{
					return JerkPostPhase(t);
				}
			}

			public double Acceleration(double t)
			{
				if (t < t1)
				{
					return jmax * t + a0;
				}
				else if (t < T12)
				{
					return amax;
				}
				else if (t < T)
				{
					return -jmax * (t - T12) + amax;
				}
				else
				{
					return 0.0;
				}
			}

			public double Velocity(double t)
			{
				if (t < t1) // Convave
				{
					return jmax / 2.0 * t * t + a0 * t + v0;
				}
				else if (t < T12) // Linear
				{
					return amax * (t - t1) + v1;
				}
				else if (t < T) // Convex
				{
					return -jmax / 2.0 * (t - T12) * (t - T12) + amax * (t - T12) + v2;
				}
				else
				{
					return vref;
				}
			}

			public double Position(double t)
			{
				if (t < t1)
				{
					return jmax / 6.0 * t * t * t + a0 / 2.0 * t * t + v0 * t;
				}
				else if (t < T12)
				{
					return amax / 2.0 * (t - t1) * (t - t1) + v1 * (t - t1) + s1;
				}
				else if (t < T)
				{
					return -jmax / 6.0 * (t - T12) * (t - T12) * (t - T12) + amax / 2.0 * (t - T12) * (t - T12) + v2 * (t - T12) + S12;
				}
				else
				{
					return S + (t - T) * vref;
				}
			}

			/*
			public (double t, double v, double a, double j) Evaluate(double distance)
			{
			}
			*/
		}

		record Point(
	long Count,
	double Current
);
	}
}
