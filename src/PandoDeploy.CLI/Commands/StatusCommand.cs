using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandoDeploy.CLI.Services;

namespace PandoDeploy.CLI.Commands;

public static class StatusCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("status", "Get server status and deployments");

        var serverOption = new Option<string>(
            "--server",
            "PandoDeploy server URL");

        var apiKeyOption = new Option<string?>(
            "--api-key",
            "API key for authentication");

        var listOption = new Option<bool>(
            "--list",
            "List recent deployments");

        var limitOption = new Option<int>(
            "--limit",
            () => 10,
            "Number of deployments to show");

        command.AddOption(serverOption);
        command.AddOption(apiKeyOption);
        command.AddOption(listOption);
        command.AddOption(limitOption);

        command.SetHandler(async (context) =>
        {
            var server = context.ParseResult.GetValueForOption(serverOption);
            var apiKey = context.ParseResult.GetValueForOption(apiKeyOption);
            var list = context.ParseResult.GetValueForOption(listOption);
            var limit = context.ParseResult.GetValueForOption(limitOption);

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var deploymentClient = serviceProvider.GetRequiredService<DeploymentClient>();

            server ??= configuration["PandoDeploy:DefaultServer"] ?? "http://localhost:5000";
            apiKey ??= Environment.GetEnvironmentVariable("PANDODEPLOY_API_KEY");

            Console.WriteLine($"Connecting to {server}...");

            var status = await deploymentClient.GetServerStatusAsync(server, apiKey);

            if (status == null)
            {
                Console.WriteLine("✗ Failed to connect to server");
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine($"\n✓ Server Status");
            Console.WriteLine($"  Version: {status.Version}");
            Console.WriteLine($"  Healthy: {(status.IsHealthy ? "Yes" : "No")}");
            Console.WriteLine($"  Server Time: {status.ServerTime:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"\n📊 Statistics");
            Console.WriteLine($"  Active Deployments: {status.ActiveDeployments}");
            Console.WriteLine($"  Total Deployments: {status.TotalDeployments}");
            Console.WriteLine($"\n🐳 Docker Status");
            Console.WriteLine($"  Available: {(status.Docker.IsAvailable ? "Yes" : "No")}");
            Console.WriteLine($"  Version: {status.Docker.Version ?? "Unknown"}");
            Console.WriteLine($"  Running Containers: {status.Docker.RunningContainers}");
            Console.WriteLine($"  Total Images: {status.Docker.TotalImages}");

            if (list)
            {
                Console.WriteLine($"\n📋 Recent Deployments");
                var deployments = await deploymentClient.GetDeploymentsAsync(server, apiKey, 0, limit);

                if (deployments == null || deployments.Deployments.Count == 0)
                {
                    Console.WriteLine("  No deployments found");
                }
                else
                {
                    foreach (var deployment in deployments.Deployments)
                    {
                        Console.WriteLine($"\n  [{deployment.Id}] {deployment.ImageName}:{deployment.ImageTag}");
                        Console.WriteLine($"    Status: {deployment.Status}");
                        Console.WriteLine($"    Created: {deployment.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                        if (!string.IsNullOrEmpty(deployment.ContainerId))
                        {
                            Console.WriteLine($"    Container: {deployment.ContainerId[..12]}");
                        }
                        if (deployment.Port.HasValue)
                        {
                            Console.WriteLine($"    Port: {deployment.Port}");
                        }
                    }
                }
            }

            context.ExitCode = 0;
        });

        return command;
    }
}

