namespace GGNet.Components;

internal sealed record TooltipContext(
  double X,
  double Y,
  double Offset,
  RenderFragment Content,
  string? Color = null,
  double? Opacity = null
);
