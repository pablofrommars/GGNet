using GGNet.Common;
using GGNet.Scales;

using static System.Math;

namespace GGNet.Data;

using static Direction;

internal sealed class Legends(Style style) : Buffer<Legend>(8, 1)
{
  private readonly Style style = style;

  public Legend GetOrAdd(IAestheticMapping aes)
  {
    for (var i = 0; i < Count; i++)
    {
      var ret = Get(i);
      if (ret.Aes == aes)
      {
        return ret;
      }
    }

    var legend = new Legend(style, aes);

    Add(legend);

    return legend;
  }

  public (double width, double height) Dimension()
  {
    var width = 0.0;
    var height = 0.0;

    for (var i = 0; i < Count; i++)
    {
      var legend = Get(i);

      if (legend.Title?.Width > 0)
      {
        var w = style.Legend.Title.Margin.Left + legend.Title.Width + style.Legend.Title.Margin.Left;
        var h = style.Legend.Title.Margin.Top + legend.Title.Height + style.Legend.Title.Margin.Bottom;

        if (style.Legend.Direction == Vertical)
        {
          width = Max(width, w);
          height += h;
        }
        else
        {
          width += w;
          height = Max(height, h);
        }
      }

      if (legend.Aes.Type == Guide.Items)
      {
        for (var j = 0; j < legend.Items.Count; j++)
        {
          var (label, elements) = legend.Items[j];

          var w = legend.Width + style.Legend.Labels.Margin.Left + label.Width + style.Legend.Labels.Margin.Right;
          var h = style.Legend.Labels.Margin.Top + Max(elements.Height, label.Height) + style.Legend.Labels.Margin.Bottom;

          if (style.Legend.Direction == Vertical)
          {
            width = Max(width, w);
            height += h;
          }
          else
          {
            width += w;
            height = Max(height, h);
          }
        }
      }
      else if (legend.Aes.Type == Guide.ColorBar)
      {
        var n = legend.Items.Count;

        if (style.Legend.Direction == Vertical)
        {
          height += style.Legend.Labels.Margin.Top;

          for (var j = 0; j < legend.Items.Count; j++)
          {
            var (label, _) = legend.Items[j];

            var w = legend.Width + style.Legend.Labels.Margin.Left + label.Width + style.Legend.Labels.Margin.Right;
            var h = Max(3.0 * legend.Height, label.Height);

            width = Max(width, w);
            height += h;
          }
        }
        else
        {
          width += style.Legend.Labels.Margin.Left + 3.0 * legend.Width * n;
          height = Max(height, legend.Items[0].label.Height + style.Legend.Labels.Margin.Bottom + legend.Height);
        }
      }
    }

    return (width, height);
  }
}
