using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Serilog;
using WorkerService;

class Program
{
    static async Task Main(string[] args)
    {

        var basePath = @"C:\MyService\logs";
        Directory.CreateDirectory(basePath);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(basePath, "log.txt"),
                rollingInterval: RollingInterval.Day)
             .WriteTo.Console()
            .CreateLogger();

        try
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .UseSerilog()
                .ConfigureServices(services =>
                {
                    services.AddHttpClient("api", c =>
                    {
                        c.BaseAddress = new Uri("https://localhost:7247");
                        c.Timeout = TimeSpan.FromSeconds(10);
                    })
                    .AddTransientHttpErrorPolicy(p =>
                         p.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2)));
                    services.AddHostedService<Worker>();
                })
                .Build();

            await host.RunAsync();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}