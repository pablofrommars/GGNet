using Microsoft.AspNetCore.Blazor.Hosting;
using System.Globalization;

namespace Site
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-GB");
            //CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
            CreateHostBuilder(args).Build().Run();
        }

        public static IWebAssemblyHostBuilder CreateHostBuilder(string[] args) =>
            BlazorWebAssemblyHost.CreateDefaultBuilder()
                .UseBlazorStartup<Startup>();
    }
}
