using DocumentManagement.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagement.Infrastructure.Data;

public sealed class DocumentDbContext(DbContextOptions<DocumentDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var document = modelBuilder.Entity<Document>();
        document.ToTable("Documents");
        document.HasKey(item => item.Id);
        document.Property(item => item.OriginalFileName).HasMaxLength(255).IsRequired();
        document.Property(item => item.StorageIdentifier).HasMaxLength(32).IsRequired();
        document.HasIndex(item => item.StorageIdentifier).IsUnique();
        document.Property(item => item.ContentType).HasMaxLength(255).IsRequired();
        document.Property(item => item.FileSize).IsRequired();
        document.Property(item => item.UploadedAtUtc).IsRequired();
    }
}
