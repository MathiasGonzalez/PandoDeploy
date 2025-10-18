using Microsoft.EntityFrameworkCore;
using PandoDeploy.Server.Data.Entities;

namespace PandoDeploy.Server.Data;

public class PandoDeployContext : DbContext
{
    public DbSet<Deployment> Deployments { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<ClientCertificate> ClientCertificates { get; set; }

    public PandoDeployContext(DbContextOptions<PandoDeployContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Deployment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ImageName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ImageTag).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => new { e.ImageName, e.ImageTag });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).IsRequired();
            entity.HasIndex(e => e.Key).IsUnique();
        });

        modelBuilder.Entity<ClientCertificate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Thumbprint).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired();
            entity.HasIndex(e => e.Thumbprint).IsUnique();
        });
    }
}

