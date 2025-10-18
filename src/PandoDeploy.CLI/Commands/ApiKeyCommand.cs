using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PandoDeploy.CLI.Commands;

public static class ApiKeyCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var command = new Command("apikey", "Manage API keys");

        var createCommand = new Command("create", "Create a new API key");
        
        var nameOption = new Option<string>(
            "--name",
            "API key name")
        { IsRequired = true };

        var serverOption = new Option<string>(
            "--server",
            "PandoDeploy server URL");

        var adminKeyOption = new Option<string?>(
            "--admin-key",
            "Admin API key for authentication");

        createCommand.AddOption(nameOption);
        createCommand.AddOption(serverOption);
        createCommand.AddOption(adminKeyOption);

        createCommand.SetHandler(async (context) =>
        {
            var name = context.ParseResult.GetValueForOption(nameOption)!;
            var server = context.ParseResult.GetValueForOption(serverOption);
            var adminKey = context.ParseResult.GetValueForOption(adminKeyOption);

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // TODO: Implement API key creation via HTTP endpoint
            Console.WriteLine("API key creation via CLI not yet implemented");
            Console.WriteLine("For now, you can create API keys directly in the database:");
            Console.WriteLine($"  INSERT INTO ApiKeys (Key, Name, IsActive, CreatedAt) VALUES ('{Guid.NewGuid():N}', '{name}', 1, datetime('now'));");
            
            context.ExitCode = 1;
        });

        command.AddCommand(createCommand);

        return command;
    }
}

