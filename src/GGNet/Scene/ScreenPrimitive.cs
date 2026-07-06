namespace GGNet.Scene;

// Screen-space primitives: final pixel coordinates and attribute values,
// composed from data-space shapes. One record per markup template in the
// Area walker, attribute-for-attribute. A closed union: the walker's
// dispatch is exhaustiveness-checked at compile time.
internal union ScreenPrimitive(ScreenCircle, ScreenLine, ScreenRect, ScreenFill, ScreenStroke, ScreenPolygon, ScreenRule, ScreenText, ScreenLabel, ScreenAngledLabel);

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

// A filled outline with no stroke or interactivity (Shapes.Area).
internal sealed record ScreenFill(
	string D,
	string Fill,
	double FillOpacity);

// A stroked path with no fill or interactivity (Shapes.Path).
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

// An annotation rule: no css class, no interactivity (V/H/AB line strokes).
internal sealed record ScreenRule(
	double X1,
	double Y1,
	double X2,
	double Y2,
	double StrokeWidth,
	string Stroke,
	double StrokeOpacity,
	LineType LineType);

// Shapes.Text template: transform second, no fill-opacity.
internal sealed record ScreenText(
	string Transform,
	string Fill,
	string Anchor,
	Size FontSize,
	string FontWeight,
	string FontStyle,
	string? Value);

// V/HLine label template: transform late.
internal sealed record ScreenLabel(
	string Fill,
	double FillOpacity,
	string Anchor,
	Size FontSize,
	string FontWeight,
	string FontStyle,
	string Transform,
	string Value);

// ABLine label template: transform first.
internal sealed record ScreenAngledLabel(
	string Transform,
	string Fill,
	double FillOpacity,
	string Anchor,
	Size FontSize,
	string FontWeight,
	string FontStyle,
	string Value);
