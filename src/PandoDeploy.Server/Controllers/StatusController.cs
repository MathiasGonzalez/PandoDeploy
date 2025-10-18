using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PandoDeploy.Core.Interfaces;
using PandoDeploy.Server.Data;
using PandoDeploy.Shared.Models;
using System.Reflection;

namespace PandoDeploy.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly ILogger<StatusController> _logger;
    private readonly IDockerService _dockerService;
    private readonly PandoDeployContext _context;

    public StatusController(
        ILogger<StatusController> logger,
        IDockerService dockerService,
        PandoDeployContext context)
    {
        _logger = logger;
        _dockerService = dockerService;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ServerStatusResponse>> GetStatus()
    {
        try
        {
            var dockerStatus = await _dockerService.GetDockerStatusAsync();
            var totalDeployments = await _context.Deployments.CountAsync();
            var activeDeployments = await _context.Deployments
                .CountAsync(d => d.Status == DeploymentStatus.Running.ToString());

            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "1.0.0";

            return Ok(new ServerStatusResponse
            {
                IsHealthy = dockerStatus.IsAvailable,
                Version = version,
                ServerTime = DateTime.UtcNow,
                Docker = dockerStatus,
                ActiveDeployments = activeDeployments,
                TotalDeployments = totalDeployments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting server status");
            return StatusCode(500, new ServerStatusResponse
            {
                IsHealthy = false,
                Version = "Unknown"
            });
        }
    }

    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck()
    {
        var isDockerAvailable = await _dockerService.IsDockerAvailableAsync();
        if (isDockerAvailable)
        {
            return Ok(new { status = "healthy" });
        }

        return StatusCode(503, new { status = "unhealthy", reason = "Docker not available" });
    }
}

