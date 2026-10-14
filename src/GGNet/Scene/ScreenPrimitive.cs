namespace GGNet.Scene;

internal readonly union ScreenPrimitive(ScreenCircle, ScreenLine, ScreenRect, ScreenFill, ScreenStroke, ScreenPolygon, ScreenRule, ScreenText, ScreenLabel, ScreenAngledLabel);

internal sealed record ScreenCircle(
	string Css,
	double X,
	double Y,
	double Radius,
	string Fill,
	double StrokeOpacity,
	double FillOpacity,
	Func<MouseEventArgs, Task> OnClick,
	Func<MouseEventArgs, Task> OnMouseOver,
	Func<MouseEventArgs, Task> OnMouseOut);

internal sealed record ScreenLine(
	string Css,
	double X1,
	double Y1,
	double X2,
	double Y2,
	double StrokeWidth,
	string Stroke,
	double StrokeOpacity,
	LineType LineType,
	Func<MouseEventArgs, Task> OnClick,
	Func<MouseEventArgs, Task> OnMouseOver,
	Func<MouseEventArgs, Task> OnMouseOut);

internal sealed record ScreenRect(
	string Css,
	double X,
	double Y,
	double Width,
	double Height,
	string Fill,
	double FillOpacity,
	string Stroke,
	double StrokeOpacity,
	double StrokeWidth,
	Func<MouseEventArgs, Task> OnClick,
	Func<MouseEventArgs, Task> OnMouseOver,
	Func<MouseEventArgs, Task> OnMouseOut);

internal sealed record ScreenFill(
	string D,
	string Fill,
	double FillOpacity);

internal sealed record ScreenStroke(
	string D,
	double StrokeWidth,
	string Stroke,
	double StrokeOpacity,
	LineType LineType);

internal sealed record ScreenPolygon(
	string Css,
	string D,
	string Fill,
	double FillOpacity,
	string Stroke,
	double StrokeWidth,
	Func<MouseEventArgs, Task> OnClick,
	Func<MouseEventArgs, Task> OnMouseOver,
	Func<MouseEventArgs, Task> OnMouseOut);

internal sealed record ScreenRule(
	double X1,
	double Y1,
	double X2,
	double Y2,
	double StrokeWidth,
	string Stroke,
	double StrokeOpacity,
	LineType LineType);

internal sealed record ScreenText(
	string Transform,
	string Fill,
	string Anchor,
	Size FontSize,
	string FontWeight,
	string FontStyle,
	string? Value);

internal sealed record ScreenLabel(
	string Fill,
	double FillOpacity,
	string Anchor,
	Size FontSize,
	string FontWeight,
	string FontStyle,
	string Transform,
	string Value);

internal sealed record ScreenAngledLabel(
	string Transform,
	string Fill,
	double FillOpacity,
	string Anchor,
	Size FontSize,
	string FontWeight,
	string FontStyle,
	string Value);
