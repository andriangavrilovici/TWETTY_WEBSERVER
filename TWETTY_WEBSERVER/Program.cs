using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TWETTY_WEBSERVER
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateBuildWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateBuildWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>();
    }
}
