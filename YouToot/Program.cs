using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Grafana.Loki;

namespace YouToot;

public class Program
{
    private static async Task Main()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var attr = Attribute.GetCustomAttribute(assembly, typeof(BuildDateTimeAttribute)) as BuildDateTimeAttribute;
        Console.WriteLine("Starting up YouToot Build " + attr?.Date);

        try
        {
            var provider = AddServices();
            var service = provider.GetRequiredService<Service>();

            Console.WriteLine("YouToot started");
            await service.TootNewVideos();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }


    private static ServiceProvider AddServices()
    {
        var services = new ServiceCollection();


        services.AddLogging(cfg => cfg.SetMinimumLevel(LogLevel.Debug));
        services.AddSerilog(cfg =>
        {
            cfg.MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("job", Assembly.GetEntryAssembly()?.GetName().Name)
                .Enrich.WithProperty("service", Assembly.GetEntryAssembly()?.GetName().Name)
                .Enrich.WithProperty("desktop", Environment.GetEnvironmentVariable("DESKTOP_SESSION"))
                .Enrich.WithProperty("language", Environment.GetEnvironmentVariable("LANGUAGE"))
                .Enrich.WithProperty("lc", Environment.GetEnvironmentVariable("LC_NAME"))
                .Enrich.WithProperty("timezone", Environment.GetEnvironmentVariable("TZ"))
                .Enrich.WithProperty("dotnetVersion", Environment.GetEnvironmentVariable("DOTNET_VERSION"))
                .Enrich.WithProperty("inContainer",
                    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"))
                .WriteTo.GrafanaLoki(Environment.GetEnvironmentVariable("LOKIURL") ?? "http://thebeast:3100",
                    propertiesAsLabels: ["job"]);
            if (Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ==
                "Debug")
            {
                cfg.WriteTo.Console(new RenderedCompactJsonFormatter());
            }
            else
            {
                cfg.WriteTo.Console();
            }
        });
        services.AddSingleton<Database>();
        services.AddScoped<Tube>();
        services.AddScoped<Toot>();
        services.AddScoped<Service>();
        services.AddSingleton(JsonConvert.DeserializeObject<Config>(File.ReadAllText("./config.json")) ??
                                      throw new FileNotFoundException("cannot read config"));

        var provider = services.BuildServiceProvider();
        return provider;
    }
}