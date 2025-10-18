using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PandoDeploy.Core.Interfaces;
using PandoDeploy.Server.Data;
using PandoDeploy.Server.Services;
using PandoDeploy.Shared.Models;
using Xunit;

namespace PandoDeploy.Server.Tests.Services;

public class DeploymentServiceTests
{
    private readonly Mock<ILogger<DeploymentService>> _loggerMock;
    private readonly Mock<IDockerService> _dockerServiceMock;
    private readonly Mock<IImageStorageService> _imageStorageServiceMock;
    private readonly PandoDeployContext _context;

    public DeploymentServiceTests()
    {
        _loggerMock = new Mock<ILogger<DeploymentService>>();
        _dockerServiceMock = new Mock<IDockerService>();
        _imageStorageServiceMock = new Mock<IImageStorageService>();

        var options = new DbContextOptionsBuilder<PandoDeployContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PandoDeployContext(options);
    }

    [Fact]
    public async Task ProcessDeploymentAsync_SuccessfulDeployment_ReturnsSuccess()
    {
        // Arrange
        var service = new DeploymentService(
            _loggerMock.Object,
            _context,
            _dockerServiceMock.Object,
            _imageStorageServiceMock.Object);

        var request = new DeploymentRequest
        {
            ImageName = "test-image",
            ImageTag = "latest",
            Port = 8080
        };

        var imageStream = new MemoryStream();
        var imagePath = "/tmp/test-image.tar.gz";
        var containerId = "abc123";

        _imageStorageServiceMock
            .Setup(x => x.SaveImageAsync(It.IsAny<Stream>(), "test-image", "latest"))
            .ReturnsAsync(imagePath);

        _imageStorageServiceMock
            .Setup(x => x.GetImageAsync(imagePath))
            .ReturnsAsync(new MemoryStream());

        _dockerServiceMock
            .Setup(x => x.LoadImageFromStreamAsync(It.IsAny<Stream>(), "test-image", "latest"))
            .ReturnsAsync("test-image:latest");

        _dockerServiceMock
            .Setup(x => x.CreateAndStartContainerAsync(request))
            .ReturnsAsync(containerId);

        // Act
        var result = await service.ProcessDeploymentAsync(imageStream, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(DeploymentStatus.Running, result.Status);
        Assert.Equal(containerId, result.ContainerId);
        
        // Verify deployment was saved to database
        var deployment = await _context.Deployments.FirstOrDefaultAsync();
        Assert.NotNull(deployment);
        Assert.Equal("test-image", deployment.ImageName);
        Assert.Equal("latest", deployment.ImageTag);
        Assert.Equal(containerId, deployment.ContainerId);
    }

    // TODO: Add more tests
    // - Test failure scenarios
    // - Test with different authentication methods
    // - Test error handling
}

