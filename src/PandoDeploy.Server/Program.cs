using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.EntityFrameworkCore;
using PandoDeploy.Core.Interfaces;
using PandoDeploy.Core.Services;
using PandoDeploy.Server.Authentication;
using PandoDeploy.Server.Data;
using PandoDeploy.Server.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/pandodeploy-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
var pandoDeployConfig = builder.Configuration.GetSection("PandoDeploy");
var dbPath = pandoDeployConfig["DatabasePath"] ?? "./data/pandodeploy.db";
var imageStoragePath = pandoDeployConfig["ImageStoragePath"] ?? "./data/images";
var dockerEndpoint = pandoDeployConfig["DockerEndpoint"] ?? "unix:///var/run/docker.sock";

// Ensure directories exist
var dbDirectory = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

if (!Directory.Exists(imageStoragePath))
{
    Directory.CreateDirectory(imageStoragePath);
}

// Add DbContext
builder.Services.AddDbContext<PandoDeployContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Add authentication
var authConfig = pandoDeployConfig.GetSection("Authentication");
var enableApiKeys = authConfig.GetValue<bool>("EnableApiKeys");
var enableMTLS = authConfig.GetValue<bool>("EnableMTLS");

var authBuilder = builder.Services.AddAuthentication();

if (enableApiKeys)
{
    authBuilder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme,
        options => 
        {
            options.ApiKeyHeaderName = authConfig["ApiKeyHeader"] ?? "X-API-Key";
        });
}

if (enableMTLS)
{
    authBuilder.AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKeyOrCertificate", policy =>
    {
        if (enableApiKeys && enableMTLS)
        {
            policy.AddAuthenticationSchemes(
                ApiKeyAuthenticationOptions.DefaultScheme,
                CertificateAuthenticationDefaults.AuthenticationScheme);
        }
        else if (enableApiKeys)
        {
            policy.AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme);
        }
        else if (enableMTLS)
        {
            policy.AddAuthenticationSchemes(CertificateAuthenticationDefaults.AuthenticationScheme);
        }
        
        policy.RequireAuthenticatedUser();
    });

    options.DefaultPolicy = options.GetPolicy("ApiKeyOrCertificate")!;
});

// Register application services
builder.Services.AddScoped<DeploymentService>();
builder.Services.AddSingleton<IDockerService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<DockerService>>();
    return new DockerService(logger, dockerEndpoint);
});
builder.Services.AddSingleton<IImageStorageService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ImageStorageService>>();
    return new ImageStorageService(logger, imageStoragePath);
});
builder.Services.AddSingleton<IImagePackerService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ImagePackerService>>();
    return new ImagePackerService(logger, dockerEndpoint);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Kestrel for specific port
var port = pandoDeployConfig.GetValue<int>("Port", 5000);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PandoDeployContext>();
    context.Database.EnsureCreated();
    
    Log.Information("Database initialized at {DbPath}", dbPath);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Log.Information("PandoDeploy Server starting on port {Port}", port);
Log.Information("Docker endpoint: {DockerEndpoint}", dockerEndpoint);
Log.Information("Image storage path: {ImageStoragePath}", imageStoragePath);
Log.Information("Authentication - API Keys: {EnableApiKeys}, mTLS: {EnableMTLS}", enableApiKeys, enableMTLS);

app.Run();

