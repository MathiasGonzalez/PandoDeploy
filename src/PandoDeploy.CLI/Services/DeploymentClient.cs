using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PandoDeploy.Shared.Models;

namespace PandoDeploy.CLI.Services;

public class DeploymentClient
{
    private readonly ILogger<DeploymentClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public DeploymentClient(
        ILogger<DeploymentClient> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;

        var timeout = configuration.GetValue<int>("PandoDeploy:Timeout", 300);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeout);
    }

    public async Task<DeploymentResponse> DeployImageAsync(
        string serverUrl,
        Stream imageStream,
        DeploymentRequest request,
        string? apiKey = null,
        string? certificatePath = null,
        string? certificatePassword = null)
    {
        try
        {
            _logger.LogInformation("Deploying {ImageName}:{ImageTag} to {Server}", 
                request.ImageName, request.ImageTag, serverUrl);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{serverUrl}/api/deployment");

            // Add authentication
            if (!string.IsNullOrEmpty(apiKey))
            {
                requestMessage.Headers.Add("X-API-Key", apiKey);
            }

            // Add metadata header
            var metadataJson = JsonSerializer.Serialize(request);
            requestMessage.Headers.Add("X-Deployment-Metadata", metadataJson);

            // Add image stream as content
            requestMessage.Content = new StreamContent(imageStream);
            requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // TODO: Implement mTLS client certificate handling
            if (!string.IsNullOrEmpty(certificatePath))
            {
                _logger.LogInformation("Using client certificate: {CertificatePath}", certificatePath);
                // This requires configuring HttpClientHandler with client certificates
            }

            var response = await _httpClient.SendAsync(requestMessage);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var deploymentResponse = JsonSerializer.Deserialize<DeploymentResponse>(responseContent);
                return deploymentResponse ?? new DeploymentResponse
                {
                    Success = false,
                    Message = "Failed to parse response"
                };
            }

            _logger.LogError("Deployment failed: {StatusCode} - {Content}", response.StatusCode, responseContent);

            return new DeploymentResponse
            {
                Success = false,
                Message = $"HTTP {response.StatusCode}",
                ErrorDetails = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during deployment");
            return new DeploymentResponse
            {
                Success = false,
                Message = "Deployment failed",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ServerStatusResponse?> GetServerStatusAsync(string serverUrl, string? apiKey = null)
    {
        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{serverUrl}/api/status");

            if (!string.IsNullOrEmpty(apiKey))
            {
                requestMessage.Headers.Add("X-API-Key", apiKey);
            }

            var response = await _httpClient.SendAsync(requestMessage);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ServerStatusResponse>(content);
            }

            _logger.LogError("Failed to get server status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting server status");
            return null;
        }
    }

    public async Task<DeploymentListResponse?> GetDeploymentsAsync(
        string serverUrl,
        string? apiKey = null,
        int skip = 0,
        int take = 50)
    {
        try
        {
            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get, 
                $"{serverUrl}/api/deployment?skip={skip}&take={take}");

            if (!string.IsNullOrEmpty(apiKey))
            {
                requestMessage.Headers.Add("X-API-Key", apiKey);
            }

            var response = await _httpClient.SendAsync(requestMessage);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DeploymentListResponse>(content);
            }

            _logger.LogError("Failed to get deployments: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deployments");
            return null;
        }
    }
}

