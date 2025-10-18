using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PandoDeploy.CLI.Commands;
using PandoDeploy.CLI.Services;
using PandoDeploy.Core.Interfaces;
using PandoDeploy.Core.Services;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables("PANDODEPLOY_")
    .Build();

// Setup DI
var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddConfiguration(configuration.GetSection("Logging"));
});

services.AddSingleton<IConfiguration>(configuration);
services.AddHttpClient<DeploymentClient>();
services.AddSingleton<DeploymentClient>();

var dockerEndpoint = configuration["PandoDeploy:DockerEndpoint"] ?? "unix:///var/run/docker.sock";
services.AddSingleton<IImagePackerService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ImagePackerService>>();
    return new ImagePackerService(logger, dockerEndpoint);
});

var serviceProvider = services.BuildServiceProvider();

// Build CLI
var rootCommand = new RootCommand("PandoDeploy - Docker deployment without registry");

rootCommand.AddCommand(DeployCommand.Create(serviceProvider));
rootCommand.AddCommand(ServerCommand.Create(serviceProvider));
rootCommand.AddCommand(StatusCommand.Create(serviceProvider));
rootCommand.AddCommand(ApiKeyCommand.Create(serviceProvider));

return await rootCommand.InvokeAsync(args);

