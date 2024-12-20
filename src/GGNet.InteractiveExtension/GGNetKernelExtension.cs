namespace GGNet.InteractiveExtension;

public static class GGNetKernelExtension
{
  public static void Load(Kernel kernel)
  {
    LoadTheme();
    LoadFormatters();
  }

  private static void LoadTheme()
  {
    var dllPath = Assembly.GetExecutingAssembly().Location;
    var extensionPathIndex = dllPath.IndexOf(@"/lib/");
    var extensionPath = dllPath.Substring(0, extensionPathIndex);
    var cssPath = Path.Combine(extensionPath, "themes", "DotnetInteractive.css");

    var css = File.ReadAllText(cssPath);

    Kernel.CSS(css);
  }

  private static void LoadFormatters()
  {
    Formatter.Register<IPlotContext>(async (context, writer) =>
    {
      context.Style ??= GGNet.Style.Default();

      var svg = await context.AsStringAsync(theme: "dotnet-interactive");

      await writer.WriteAsync(svg);
    }, "text/html");
  }
}
