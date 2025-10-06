using Microsoft.EntityFrameworkCore;
using McpGateway.Domain.Entities;

namespace McpGateway.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for the MCP Gateway application.
/// Manages database operations and entity configurations for MCP adapters.
/// </summary>
public class McpGatewayDbContext(DbContextOptions<McpGatewayDbContext> options) : DbContext(options)
{
    public DbSet<McpAdapterEntity> McpAdapters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure McpAdapterEntity
        modelBuilder.Entity<McpAdapterEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired().HasConversion<int>();
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.Property(e => e.LastError).HasMaxLength(1000);
        });
    }
}
