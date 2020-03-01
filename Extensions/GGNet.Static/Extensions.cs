using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

using GGNet.Components;

namespace GGNet.Static
{
    public static class Extensions
    {
        private static async Task Render<T, TX, TY>(Data<T, TX, TY> data, TextWriter writer, double width = 720, double height = 576)
           where TX : struct
           where TY : struct
        {
            var component = await Host.Instance.RenderAsync<Plot<T, TX, TY>>(new Dictionary<string, object>
            {
                ["Data"] = data,
                ["Width"] = width,
                ["Height"] = height
            });

            component.WriteHTML(writer);
        }

        public static Task Save<T, TX, TY>(this Data<T, TX, TY> data, string fn, double width = 720, double height = 576)
           where TX : struct
           where TY : struct
        {
            using var writer = File.CreateText(fn);
            return Render(data, writer, width, height);
        }

        public static async Task<string> AsString<T, TX, TY>(this Data<T, TX, TY> data, double width = 720, double height = 576)
           where TX : struct
           where TY : struct
        {
            using var writer = new StringWriter();
            await Render(data, writer, width, height);
            return writer.ToString();
        }
    }
}
