using System.ComponentModel.DataAnnotations;
using DocumentManagement.Domain.Models;

namespace DocumentManagement.Application.Documents;

public sealed record DocumentUpload(
    [property: Required, StringLength(255)] string FileName,
    [property: Required, StringLength(255)] string ContentType,
    [property: Range(1, DocumentUpload.MaxFileSizeBytes)] long FileSize,
    [property: Required] Stream Content)
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
}

public sealed record DocumentDetails(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    DateTimeOffset UploadedAtUtc);

public sealed record DocumentDownload(
    DocumentDetails Details,
    Stream Content);

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IDocumentStorage
{
    Task StoreAsync(string storageIdentifier, Stream content, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storageIdentifier, CancellationToken cancellationToken);
    Task DeleteAsync(string storageIdentifier, CancellationToken cancellationToken);
}
