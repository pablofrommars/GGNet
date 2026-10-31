using GGNet.Scales;

namespace GGNet.Headless.Tests;

public class TextSelectorTests
{
	public sealed record Row(double X, double Y, double? Label);

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void EveryOverloadDefersLabelsAndReevaluatesOncePerRender(int overload)
	{
		// Arrange

		Row[] rows = [new(1, 2, 1.25)];
		var calls = 0;
		Func<Row, double?> selector = row =>
		{
			calls++;
			return row.Label;
		};
		var context = PlotContext.Build(rows, row => row.X, row => row.Y);
		switch (overload)
		{
			case 0:
				context.Geom_Text(text: selector);
				break;
			case 1:
				context.Default_Panel().Geom_Text(text: selector);
				break;
			case 2:
				context.Geom_Text(rows, row => row.X, row => row.Y, text: selector);
				break;
			case 3:
				context.Default_Panel().Geom_Text(rows, row => row.X, row => row.Y, text: selector);
				break;
		}
		context.Style();
		context.Init();
		var callsBeforeRendering = calls;

		// Act

		context.Render();
		var first = Texts(context);
		var callsAfterFirstRender = calls;
		rows[0] = rows[0] with { Label = 9.5 };
		context.Render();

		// Assert

		using var _ = new AssertionScope();

		callsBeforeRendering.Should().Be(0);
		callsAfterFirstRender.Should().Be(1);
		calls.Should().Be(2);
		first.Should().Equal("1.25");
		Texts(context).Should().Equal("9.5");
	}

	[Fact]
	public void FormatsReferenceLabelsInvariantlyBeforeMappingColorAndAngle()
	{
		// Arrange

		Row[] rows = [new(1, 2, 1.25)];
		List<string> events = [];
		IFormatProvider? observedProvider = null;
		var label = new Label(provider =>
		{
			events.Add("format");
			observedProvider = provider;
			return 1.25.ToString(provider);
		});
		var color = new Mock<IAestheticMapping<Row, string>>();
		color.Setup(mapping => mapping.Map(It.IsAny<Row>()))
			.Callback(() => events.Add("color"))
			.Returns("blue");
		var context = PlotContext.Build(rows, row => row.X, row => row.Y)
			.Geom_Text(text: row =>
			{
				events.Add("select");
				return label;
			}, colorBy: color.Object, angleBy: row =>
			{
				events.Add("angle");
				return 0;
			});
		context.Style();

		// Act

		context.Render();

		// Assert

		using var _ = new AssertionScope();

		events.Should().Equal("select", "format", "color", "angle");
		observedProvider.Should().BeSameAs(CultureInfo.InvariantCulture);
		Texts(context).Should().Equal("1.25");
	}

	[Fact]
	public void NullableValueLabelsSkipNullButKeepZero()
	{
		// Arrange

		Row[] rows = [new(1, 2, null), new(2, 3, 0)];
		var context = PlotContext.Build(rows, row => row.X, row => row.Y)
			.Geom_Text(text: row => row.Label);
		context.Style();

		// Act

		context.Render();

		// Assert

		Texts(context).Should().Equal("0");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void NullAndEmptyReferenceLabelsProduceNoShapes(string? label)
	{
		// Arrange

		Row[] rows = [new(1, 2, 1)];
		var context = PlotContext.Build(rows, row => row.X, row => row.Y)
			.Geom_Text(text: row => label);
		context.Style();

		// Act

		context.Render();

		// Assert

		context.Panels[0].Geoms[0].Layer.Should().BeEmpty();
	}

	[Fact]
	public void MissingSelectorProducesNoShapes()
	{
		// Arrange

		Row[] rows = [new(1, 2, 1)];
		var context = PlotContext.Build(rows, row => row.X, row => row.Y)
			.Geom_Text(text: (Func<Row, double>?)null);
		context.Style();

		// Act

		context.Render();

		// Assert

		context.Panels[0].Geoms[0].Layer.Should().BeEmpty();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SelectorAndFormatterFailuresPropagateOnlyWhenRendering(bool formatterFails)
	{
		// Arrange

		Row[] rows = [new(1, 2, 1)];
		var failure = new InvalidOperationException("Label failed");
		var label = new Label(provider => throw failure);
		var context = PlotContext.Build(rows, row => row.X, row => row.Y)
			.Geom_Text(text: row => formatterFails ? label : throw failure);
		context.Style();
		context.Init();

		// Act

		Action render = context.Render;

		// Assert

		render.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(failure);
	}

	private static string?[] Texts(PlotContext<Row, double, double> context)
		=> context.Panels[0].Geoms[0].Layer.Select(shape => shape.Value).OfType<Shapes.Text>().Select(text => text.Value).ToArray();

	private sealed class Label(Func<IFormatProvider?, string> format) : IFormattable
	{
		public string ToString(string? formatString, IFormatProvider? formatProvider) => format(formatProvider);

		public override string ToString() => throw new InvalidOperationException("Use IFormattable with an invariant provider");
	}
}
