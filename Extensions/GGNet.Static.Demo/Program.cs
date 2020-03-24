using System.Threading.Tasks;

using GGNet.Datasets;

namespace GGNet.Static.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var plot = Plot.New(Tip.Load(), x: o => o.Day, y: o => o.Avg)
                .Geom_ErrorBar(ymin: o => o.Lower, ymax: o => o.Upper, position: PositionAdjustment.Dodge)
                .Scale_Color_Discrete(o => o.Sex, new[] { "#69b3a2", "#404080" })
                .YLab("Tip (%)")
                .Theme(dark: false);

            //Save to File
            await plot.Save("tip.svg");

            //Render as string
            var svg = await plot.AsString();
        }
    }
}
