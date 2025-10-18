using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PandoDeploy.Server.Services;
using PandoDeploy.Shared.Models;
using System.Text.Json;

namespace PandoDeploy.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeploymentController : ControllerBase
{
    private readonly ILogger<DeploymentController> _logger;
    private readonly DeploymentService _deploymentService;

    public DeploymentController(
        ILogger<DeploymentController> logger,
        DeploymentService deploymentService)
    {
        _logger = logger;
        _deploymentService = deploymentService;
    }

    [HttpPost]
    public async Task<ActionResult<DeploymentResponse>> Deploy()
    {
        try
        {
            // Read metadata from header
            if (!Request.Headers.TryGetValue("X-Deployment-Metadata", out var metadataHeader))
            {
                return BadRequest("Missing X-Deployment-Metadata header");
            }

            var request = JsonSerializer.Deserialize<DeploymentRequest>(metadataHeader.ToString());
            if (request == null)
            {
                return BadRequest("Invalid deployment metadata");
            }

            // Get authentication info
            var apiKeyId = User.FindFirst("ApiKeyId")?.Value;
            var certThumbprint = User.FindFirst("CertificateThumbprint")?.Value;

            // Process the image stream from request body
            var response = await _deploymentService.ProcessDeploymentAsync(
                Request.Body,
                request,
                apiKeyId,
                certThumbprint);

            if (response.Success)
            {
                return Ok(response);
            }

            return StatusCode(500, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deployment");
            return StatusCode(500, new DeploymentResponse
            {
                Success = false,
                Message = "Internal server error",
                ErrorDetails = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DeploymentInfo>> GetDeployment(int id)
    {
        var deployment = await _deploymentService.GetDeploymentAsync(id);
        if (deployment == null)
        {
            return NotFound();
        }

        return Ok(deployment);
    }

    [HttpGet]
    public async Task<ActionResult<DeploymentListResponse>> GetDeployments([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var response = await _deploymentService.GetDeploymentsAsync(skip, take);
        return Ok(response);
    }
}

