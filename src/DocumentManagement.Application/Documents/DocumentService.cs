using System.ComponentModel.DataAnnotations;
using DocumentManagement.Domain.Models;

namespace DocumentManagement.Application.Documents;

public sealed class DocumentService(
    IDocumentRepository repository,
    IDocumentStorage storage)
{
    public async Task<DocumentDetails> UploadAsync(
        DocumentUpload upload,
        CancellationToken cancellationToken)
    {
        ValidateUpload(upload);

        var id = Guid.NewGuid();
        var storageIdentifier = id.ToString("N");
        var document = new Document(
            id,
            upload.FileName,
            storageIdentifier,
            upload.ContentType,
            upload.FileSize,
            DateTimeOffset.UtcNow);

        await storage.StoreAsync(storageIdentifier, upload.Content, cancellationToken);
        await repository.AddAsync(document, cancellationToken);

        return ToDetails(document);
    }

    public async Task<IReadOnlyList<DocumentDetails>> ListAsync(CancellationToken cancellationToken)
    {
        var documents = await repository.GetAllAsync(cancellationToken);
        return documents.Select(ToDetails).ToArray();
    }

    public async Task<DocumentDetails> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new DocumentNotFoundException(id);

        return ToDetails(document);
    }

    public async Task<DocumentDownload> DownloadAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new DocumentNotFoundException(id);

        var content = await storage.OpenReadAsync(document.StorageIdentifier, cancellationToken)
            ?? throw new DocumentNotFoundException(id);

        return new DocumentDownload(ToDetails(document), content);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new DocumentNotFoundException(id);

        if (!await repository.DeleteAsync(id, cancellationToken))
        {
            throw new DocumentNotFoundException(id);
        }

        await storage.DeleteAsync(document.StorageIdentifier, CancellationToken.None);
    }

    private static void ValidateUpload(DocumentUpload upload)
    {
        if (upload.FileSize > DocumentUpload.MaxFileSizeBytes)
        {
            throw new DocumentTooLargeException(DocumentUpload.MaxFileSizeBytes);
        }

        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(
                upload,
                new ValidationContext(upload),
                validationResults,
                validateAllProperties: true))
        {
            throw new DocumentValidationException(
                validationResults[0].ErrorMessage ?? "The uploaded file is invalid.");
        }
    }

    private static DocumentDetails ToDetails(Document document) =>
        new(
            document.Id,
            document.OriginalFileName,
            document.ContentType,
            document.FileSize,
            document.UploadedAtUtc);
}
