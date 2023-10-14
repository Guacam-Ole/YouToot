using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using YouToot;

var assembly = System.Reflection.Assembly.GetExecutingAssembly();
var attr = Attribute.GetCustomAttribute(assembly, typeof(BuildDateTimeAttribute)) as BuildDateTimeAttribute;
Console.WriteLine("Starting up RSSBot Build " + attr?.Date);

var provider = AddServices();
var service = provider.GetRequiredService<Service>();

try
{
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
                await service.TootVideoSinceId(args[1]);
                break;

            default:
                CallForHelp();
                return;
        }
    }
    Environment.Exit(0);

}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

void CallForHelp()
{
    Console.WriteLine("YouToot                   Auto-Mode: Post new Videos. If this is the first start no video is posted until the next call");
    Console.WriteLine("YouToot /reset            Reset. Forget about everything and start fresh next time");
    Console.WriteLine("YouToot /number [count]   Toot the newest [count] videos");
    Console.WriteLine("YouToot /sinceid [id]     Toot all videos since id [id]");
}

ServiceProvider AddServices()
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