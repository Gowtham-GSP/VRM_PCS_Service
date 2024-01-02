using VRM_PCS_SERVICE;
using VRM_PCS_SERVICE.Services;
using Serilog;
using VRM_PCS_SERVICE.Interface;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostingContext.Configuration)
    .Enrich.FromLogContext())
    .ConfigureServices((context, services) =>
    {
        services.AddOptions<Configurations>().Bind(context.Configuration.GetSection(Configurations.SectionName)).ValidateDataAnnotations();
        services.AddTransient<IServiceEngine, ServiceEngine>();
        services.AddTransient<IDBHelper, DBHelper>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();