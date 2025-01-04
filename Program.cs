using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;

namespace YouToot;

public class Program
{
    private static async Task Main(string[] args)
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

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
            var logFile = "youtoot.log";
            logging.AddFile(logFile, conf =>
            {
                conf.Append = true;
                conf.MaxRollingFiles = 1;
                conf.FileSizeLimitBytes = 100000;
            });
        });
        services.AddSingleton<Database>();
        services.AddScoped<Tube>();
        services.AddScoped<Toot>();
        services.AddScoped<Service>();

        var provider = services.BuildServiceProvider();
        return provider;
    }

   
}