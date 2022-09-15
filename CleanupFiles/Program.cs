using CleanupFiles;
using CleanupFiles.Extensions;
using CleanupFiles.Models;
using Quartz;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(context.Configuration.GetSection("CleanupSettings").Get<CleanupSettings>());
        services.AddHostedService<Worker>();
        services.AddQuartz(x =>
        {
            x.UseMicrosoftDependencyInjectionJobFactory();
            x.AddJobAndTrigger<CleanUpJob>(context.Configuration);
        });
        services.AddQuartzHostedService(x => x.WaitForJobsToComplete = true);
    })
    .ConfigureLogging((context, builder) =>
    {
        builder.ClearProviders();
    })
    .UseSerilog((context, config) =>
    {
        config.ReadFrom.Configuration(context.Configuration);
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
