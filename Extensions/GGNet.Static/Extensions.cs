using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GGNet.Static
{
    public static class Extensions
    {
        private static async Task Render(IData data, TextWriter writer, double width = 720, double height = 576)
        {
            var component = await Host.Instance.RenderAsync(data.PlotType, new Dictionary<string, object>
            {
                ["Data"] = data,
                ["Width"] = width,
                ["Height"] = height
            });

            component.WriteHTML(writer);
        }

        public static Task Save(this IData data, string fn, double width = 720, double height = 576)
        {
            using var writer = File.CreateText(fn);
            return Render(data, writer, width, height);
        }

        public static async Task<string> AsString(this IData data, double width = 720, double height = 576)
        {
            using var writer = new StringWriter();
            await Render(data, writer, width, height);
            return writer.ToString();
        }
    }
}
