using System.Xml.Linq;

namespace GGNet.Headless.Tests;

// The overload families are hand-copied by design (the flat DSL), which makes
// them the surface's single source of potential untruth: a default, parameter
// shape, order or doc drifting between siblings. These tests are the
// consistency guarantee — the source-generator alternative is permanently
// retired (Pablo, 2026-07-06): verify, don't generate.
public class OverloadConsistencyTests
{
	private static readonly Type[] surfaces = [typeof(BuilderExtensions), typeof(PlotContext), typeof(Stat)];

	private static IEnumerable<IGrouping<string, MethodInfo>> Families()
		=> surfaces
			.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
			.Where(m => !m.IsSpecialName)
			.GroupBy(m => $"{m.DeclaringType!.Name}.{m.Name}")
			.Where(g => g.Count() > 1);

	// Generic slots are erased to a wildcard: the same parameter binds T2/TX1
	// in source overloads and T/TX in context overloads. This is blind to
	// slot swaps by construction; the defaults check is the sharp edge.
	private static string Shape(Type type)
	{
		if (type.IsGenericParameter)
		{
			return "§";
		}

		if (type.IsArray)
		{
			return Shape(type.GetElementType()!) + "[]";
		}

		if (type.IsGenericType)
		{
			var name = type.GetGenericTypeDefinition().Name;

			return $"{name[..name.IndexOf('`')]}<{string.Join(", ", type.GetGenericArguments().Select(Shape))}>";
		}

		return type.Name;
	}

	private static bool IsReceiver(ParameterInfo p) => p.Position == 0 && p.Member.IsDefined(typeof(ExtensionAttribute), false);

	// A generic slot may specialize to a concrete type across overloads (the
	// double-axis convenience overloads); two differing concrete shapes are
	// drift.
	private static bool Compatible(Type a, Type b)
	{
		if (a.IsGenericParameter || b.IsGenericParameter)
		{
			return true;
		}

		if (a.IsArray || b.IsArray)
		{
			return a.IsArray && b.IsArray && Compatible(a.GetElementType()!, b.GetElementType()!);
		}

		if (a.IsGenericType || b.IsGenericType)
		{
			return a.IsGenericType && b.IsGenericType
				&& a.GetGenericTypeDefinition() == b.GetGenericTypeDefinition()
				&& a.GetGenericArguments().Zip(b.GetGenericArguments()).All(pair => Compatible(pair.First, pair.Second));
		}

		return a == b;
	}

	[Fact]
	public void DefaultsAgreeAcrossEachFamily()
	{
		// Arrange

		var violations = new List<string>();

		// Act

		foreach (var family in Families())
		{
			var defaults = new Dictionary<string, (object? value, string where)>();

			foreach (var method in family)
			{
				foreach (var p in method.GetParameters().Where(p => !IsReceiver(p) && p.HasDefaultValue))
				{
					if (defaults.TryGetValue(p.Name!, out var seen))
					{
						if (!Equals(seen.value, p.DefaultValue))
						{
							violations.Add($"{family.Key}.{p.Name}: '{seen.value}' vs '{p.DefaultValue}'");
						}
					}
					else
					{
						defaults[p.Name!] = (p.DefaultValue, family.Key);
					}
				}
			}
		}

		// Assert

		violations.Should().BeEmpty();
	}

	[Fact]
	public void ParameterShapesAgreeAcrossEachFamily()
	{
		// Arrange

		var violations = new List<string>();

		// Act

		// Shapes are compared within (name, receiver-shape) sub-families:
		// XLim/Scale_* overloads dispatch on the axis type via the receiver's
		// closed generics, and Build dispatches on its selector types outright.
		var subFamilies = surfaces
			.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
			.Where(m => !m.IsSpecialName && m.Name != "Build")
			.GroupBy(m =>
			{
				var receiver = m.GetParameters().FirstOrDefault(IsReceiver);

				return $"{m.DeclaringType!.Name}.{m.Name}[{(receiver is null ? "" : Shape(receiver.ParameterType))}]";
			})
			.Where(g => g.Count() > 1);

		foreach (var family in subFamilies)
		{
			var shapes = new Dictionary<string, Type>();

			foreach (var method in family)
			{
				// The data-bearing dispatch parameters vary by design: source
				// (Source/IEnumerable/IReadOnlyList), palette (typed vs raw
				// array), polygons (selector vs direct array on Geom_Map).
				foreach (var p in method.GetParameters().Where(p => !IsReceiver(p) && p.Name is not ("source" or "palette" or "polygons")))
				{
					if (shapes.TryGetValue(p.Name!, out var seen))
					{
						if (!Compatible(seen, p.ParameterType))
						{
							violations.Add($"{family.Key}.{p.Name}: '{Shape(seen)}' vs '{Shape(p.ParameterType)}'");
						}
					}
					else
					{
						shapes[p.Name!] = p.ParameterType;
					}
				}
			}
		}

		// Assert

		violations.Should().BeEmpty();
	}

	[Fact]
	public void SugarOverloadsPreserveTheCanonicalParameterOrder()
	{
		// Arrange

		var violations = new List<string>();

		// Act

		foreach (var family in Families())
		{
			var canonical = family
				.OrderByDescending(m => m.GetParameters().Length)
				.First()
				.GetParameters()
				.Where(p => !IsReceiver(p))
				.Select(p => p.Name!)
				.ToList();

			foreach (var method in family)
			{
				var previous = -1;

				foreach (var p in method.GetParameters().Where(p => !IsReceiver(p)))
				{
					var index = canonical.IndexOf(p.Name!);

					if (index < 0)
					{
						continue;
					}

					if (index < previous)
					{
						violations.Add($"{family.Key}: '{p.Name}' out of canonical order");
					}

					previous = index;
				}
			}
		}

		// Assert

		violations.Should().BeEmpty();
	}

	[Fact]
	public void DocumentationAgreesAcrossEachFamily()
	{
		// Arrange

		var xml = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "GGNet.xml"));

		var violations = new List<string>();

		// Act

		var members = xml.Root!
			.Element("members")!
			.Elements("member")
			.Where(m => m.Attribute("name")!.Value.StartsWith("M:", StringComparison.Ordinal));

		var families = members
			.Select(m => (name: m.Attribute("name")!.Value, element: m))
			.Select(m => (family: m.name[2..(m.name.IndexOf('(') is var i && i > 0 ? i : m.name.Length)].Split('`')[0], m.element))
			.GroupBy(m => m.family);

		foreach (var family in families)
		{
			var texts = new Dictionary<string, string>();

			foreach (var (_, element) in family)
			{
				foreach (var param in element.Elements("param"))
				{
					var name = param.Attribute("name")!.Value;
					var text = string.Concat(param.Nodes().Select(n => n.ToString())).Trim();

					if (texts.TryGetValue(name, out var seen))
					{
						if (seen != text)
						{
							violations.Add($"{family.Key}.{name}");
						}
					}
					else
					{
						texts[name] = text;
					}
				}
			}
		}

		// Assert

		violations.Should().BeEmpty();
	}
}
