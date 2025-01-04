using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace YouToot;

public class Program
{
    private static async Task CheckArgs(string[] args)
    {
        try
        {
            var provider = AddServices();
            var service = provider.GetRequiredService<Service>();
            if (args.Length == 0)
            {
                Console.WriteLine("Auto-Mode. Will toot new Videos. Call with /help for different options");
                await service.TootNewVIdeos();
            }
            else
            {
                switch (args[0])
                {
                    case "/reset":
                        break;

                    case "/number":
                        if (args.Length == 1 || !int.TryParse(args[1], out int numberOfVideos))
                        {
                            CallForHelp();
                            return;
                        }
                        await service.TootLastVideos(numberOfVideos);
                        break;

                    case "/sinceid":
                        if (args.Length == 1)
                        {
                            CallForHelp();
                            return;
                        }
                        await service.TootVideoSinceId(new List<string> { args[1] });
                        break;

                    default:
                        CallForHelp();
                        return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private static async Task Main(string[] args)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var attr = Attribute.GetCustomAttribute(assembly, typeof(BuildDateTimeAttribute)) as BuildDateTimeAttribute;
        Console.WriteLine("Starting up RSSBot Build " + attr?.Date);

        var timer = new Timer(WatchDog, null, TimeSpan.FromMinutes(5), TimeSpan.Zero);
        Thread.Sleep(TimeSpan.FromMinutes(10));
        await CheckArgs(args);
        Environment.Exit(0);
    }

    private static void CallForHelp()
    {
        Console.WriteLine("YouToot                   Auto-Mode: Post new Videos. If this is the first start no video is posted until the next call");
        Console.WriteLine("YouToot /reset            Reset. Forget about everything and start fresh next time");
        Console.WriteLine("YouToot /number [count]   Toot the newest [count] videos");
        Console.WriteLine("YouToot /sinceid [id]     Toot all videos since id [id]");
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
            logging.AddFile(logFile, conf => { conf.Append = true; conf.MaxRollingFiles = 1; conf.FileSizeLimitBytes = 100000; });
        });
        services.AddSingleton<Database>();
        services.AddScoped<Tube>();
        services.AddScoped<Toot>();
        services.AddScoped<Service>();

        var provider = services.BuildServiceProvider();
        return provider;
    }

    private static void WatchDog(object? state)
    {
        Console.WriteLine("WATCHDOG: Process seems to be hanging. Exiting");
        Environment.Exit(-1);
    }
}