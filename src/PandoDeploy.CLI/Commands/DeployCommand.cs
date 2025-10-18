using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandoDeploy.CLI.Services;
using PandoDeploy.Core.Interfaces;
using PandoDeploy.Shared.Models;

namespace PandoDeploy.CLI.Commands;

public static class DeployCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("deploy", "Deploy a Docker image to PandoDeploy server");

        var imageOption = new Option<string>(
            "--image",
            "Docker image name and tag (e.g., myapp:latest)")
        { IsRequired = true };

        var serverOption = new Option<string>(
            "--server",
            "PandoDeploy server URL");

        var apiKeyOption = new Option<string?>(
            "--api-key",
            "API key for authentication");

        var portOption = new Option<int?>(
            "--port",
            "Port to expose the container");

        var envOption = new Option<string[]?>(
            "--env",
            "Environment variables (format: KEY=VALUE)")
        { AllowMultipleArgumentsPerToken = true };

        var labelOption = new Option<string[]?>(
            "--label",
            "Container labels (format: KEY=VALUE)")
        { AllowMultipleArgumentsPerToken = true };

        var containerNameOption = new Option<string?>(
            "--name",
            "Container name");

        var volumeOption = new Option<string[]?>(
            "--volume",
            "Volume mounts (format: host:container)")
        { AllowMultipleArgumentsPerToken = true };

        var certOption = new Option<string?>(
            "--cert",
            "Client certificate path for mTLS");

        var certPasswordOption = new Option<string?>(
            "--cert-password",
            "Client certificate password");

        command.AddOption(imageOption);
        command.AddOption(serverOption);
        command.AddOption(apiKeyOption);
        command.AddOption(portOption);
        command.AddOption(envOption);
        command.AddOption(labelOption);
        command.AddOption(containerNameOption);
        command.AddOption(volumeOption);
        command.AddOption(certOption);
        command.AddOption(certPasswordOption);

        command.SetHandler(async (context) =>
        {
            var image = context.ParseResult.GetValueForOption(imageOption)!;
            var server = context.ParseResult.GetValueForOption(serverOption);
            var apiKey = context.ParseResult.GetValueForOption(apiKeyOption);
            var port = context.ParseResult.GetValueForOption(portOption);
            var envVars = context.ParseResult.GetValueForOption(envOption);
            var labels = context.ParseResult.GetValueForOption(labelOption);
            var containerName = context.ParseResult.GetValueForOption(containerNameOption);
            var volumes = context.ParseResult.GetValueForOption(volumeOption);
            var cert = context.ParseResult.GetValueForOption(certOption);
            var certPassword = context.ParseResult.GetValueForOption(certPasswordOption);

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var deploymentClient = serviceProvider.GetRequiredService<DeploymentClient>();
            var imagePackerService = serviceProvider.GetRequiredService<IImagePackerService>();

            // Use default server if not provided
            server ??= configuration["PandoDeploy:DefaultServer"] ?? "http://localhost:5000";

            // Get API key from environment if not provided
            apiKey ??= Environment.GetEnvironmentVariable("PANDODEPLOY_API_KEY");

            // Parse image name and tag
            var parts = image.Split(':');
            var imageName = parts[0];
            var imageTag = parts.Length > 1 ? parts[1] : "latest";

            logger.LogInformation("Packing image {ImageName}:{ImageTag}", imageName, imageTag);
            Console.WriteLine($"📦 Packing image {imageName}:{imageTag}...");

            // Pack the image
            Stream imageStream;
            try
            {
                imageStream = await imagePackerService.PackImageAsync(imageName, imageTag, context.GetCancellationToken());
                Console.WriteLine($"✓ Image packed successfully ({imageStream.Length} bytes)");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to pack image");
                Console.WriteLine($"✗ Failed to pack image: {ex.Message}");
                context.ExitCode = 1;
                return;
            }

            // Build deployment request
            var request = new DeploymentRequest
            {
                ImageName = imageName,
                ImageTag = imageTag,
                Port = port,
                ContainerName = containerName,
                Volumes = volumes?.ToList() ?? new List<string>()
            };

            // Parse environment variables
            if (envVars != null)
            {
                foreach (var env in envVars)
                {
                    var envParts = env.Split('=', 2);
                    if (envParts.Length == 2)
                    {
                        request.EnvironmentVariables[envParts[0]] = envParts[1];
                    }
                }
            }

            // Parse labels
            if (labels != null)
            {
                foreach (var label in labels)
                {
                    var labelParts = label.Split('=', 2);
                    if (labelParts.Length == 2)
                    {
                        request.Labels[labelParts[0]] = labelParts[1];
                    }
                }
            }

            logger.LogInformation("Deploying to server {Server}", server);
            Console.WriteLine($"🚀 Deploying to {server}...");

            var response = await deploymentClient.DeployImageAsync(
                server,
                imageStream,
                request,
                apiKey,
                cert,
                certPassword);

            if (response.Success)
            {
                Console.WriteLine($"✓ Deployment successful!");
                Console.WriteLine($"  Deployment ID: {response.DeploymentId}");
                Console.WriteLine($"  Container ID: {response.ContainerId}");
                Console.WriteLine($"  Status: {response.Status}");
                context.ExitCode = 0;
            }
            else
            {
                Console.WriteLine($"✗ Deployment failed: {response.Message}");
                if (!string.IsNullOrEmpty(response.ErrorDetails))
                {
                    Console.WriteLine($"  Details: {response.ErrorDetails}");
                }
                context.ExitCode = 1;
            }
        });

        return command;
    }
}

