using DocumentManagement.Application.Documents;
using DocumentManagement.Infrastructure.Data;
using DocumentManagement.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DocumentManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString("DocumentManagement");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure the 'ConnectionStrings:DocumentManagement' connection string.");
        }

        services.AddDbContext<DocumentDbContext>(options => options.UseSqlServer(connectionString));
        var storageOptions = new DocumentStorageOptions
        {
            RootPath = configuration["DocumentStorage:RootPath"] ?? "App_Data/documents"
        };
        storageOptions.RootPath = Path.GetFullPath(
            Path.IsPathRooted(storageOptions.RootPath)
                ? storageOptions.RootPath
                : Path.Combine(contentRootPath, storageOptions.RootPath));
        services.AddSingleton(Options.Create(storageOptions));
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddSingleton<IDocumentStorage, LocalDocumentStorage>();
        return services;
    }
}
