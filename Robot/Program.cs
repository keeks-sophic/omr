using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Robot.Options;
using Robot.Services;
using Robot.Workers;

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        var mappings = new Dictionary<string, string>
        {
            { "-n", "Robot:Name" },
            { "--name", "Robot:Name" },
            { "--ip", "Robot:Ip" },
            { "--iface", "Robot:Interface" },
            { "--interface", "Robot:Interface" },
            { "-i", "Robot:Interface" }
            ,{ "--nats", "Robot:NatsUrl" }
            ,{ "--cmd", "Robot:CommandSubject" }
            ,{ "--tele", "Robot:TelemetrySubject" }
            ,{ "--ident", "Robot:IdentitySubject" }
            ,{ "--ident-stream", "Robot:IdentityStream" }
            ,{ "--tele-stream", "Robot:TelemetryStream" }
            ,{ "--cmd-stream", "Robot:CommandStream" }
        };
        config.AddCommandLine(args, mappings);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<RobotOptions>(context.Configuration.GetSection("Robot"));
        services.AddSingleton<IdentityService>();
        services.AddSingleton<CommandListenerService>();
        services.AddSingleton<NatsService>();
        services.AddSingleton<TelemetryService>();
        services.AddHostedService<RobotWorker>();
    })
    .Build()
    .Run();
