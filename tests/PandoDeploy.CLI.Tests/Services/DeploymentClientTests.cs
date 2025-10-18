using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PandoDeploy.CLI.Services;
using Xunit;

namespace PandoDeploy.CLI.Tests.Services;

public class DeploymentClientTests
{
    private readonly Mock<ILogger<DeploymentClient>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;

    public DeploymentClientTests()
    {
        _loggerMock = new Mock<ILogger<DeploymentClient>>();
        _configurationMock = new Mock<IConfiguration>();
        
        _configurationMock
            .Setup(x => x["PandoDeploy:Timeout"])
            .Returns("300");
    }

    [Fact]
    public void Constructor_InitializesHttpClient()
    {
        // Arrange & Act
        var httpClient = new HttpClient();
        var client = new DeploymentClient(_loggerMock.Object, httpClient, _configurationMock.Object);

        // Assert
        Assert.NotNull(client);
    }

    // TODO: Add more tests
    // - Test deployment API calls
    // - Test authentication headers
    // - Test error handling
    // - Test status retrieval
}

