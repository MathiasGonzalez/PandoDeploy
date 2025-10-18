using System.CommandLine;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PandoDeploy.CLI.Commands;

public static class ServerCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("server", "Start PandoDeploy server");

        var portOption = new Option<int>(
            "--port",
            () => 5000,
            "Port to listen on");

        var configOption = new Option<string?>(
            "--config",
            "Path to appsettings.json");

        var installServiceOption = new Option<bool>(
            "--install-service",
            "Install as system service (Linux/Windows)");

        command.AddOption(portOption);
        command.AddOption(configOption);
        command.AddOption(installServiceOption);

        command.SetHandler(async (context) =>
        {
            var port = context.ParseResult.GetValueForOption(portOption);
            var config = context.ParseResult.GetValueForOption(configOption);
            var installService = context.ParseResult.GetValueForOption(installServiceOption);

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            if (installService)
            {
                // TODO: Implement service installation
                Console.WriteLine("Service installation not yet implemented");
                Console.WriteLine("For now, you can run the server directly:");
                Console.WriteLine($"  dotnet PandoDeploy.Server.dll --urls http://0.0.0.0:{port}");
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine("Starting PandoDeploy server...");
            Console.WriteLine($"Port: {port}");
            
            if (!string.IsNullOrEmpty(config))
            {
                Console.WriteLine($"Config: {config}");
            }

            // TODO: Implement server start logic
            // This would require embedding the server or launching it as a separate process
            Console.WriteLine("Server start not yet implemented in CLI");
            Console.WriteLine("Please use: dotnet run --project src/PandoDeploy.Server");
            
            context.ExitCode = 1;
        });

        return command;
    }
}

