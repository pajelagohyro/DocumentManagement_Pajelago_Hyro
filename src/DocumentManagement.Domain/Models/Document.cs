namespace DocumentManagement.Domain.Models;

public sealed class Document
{
    public Guid Id { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StorageIdentifier { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public DateTimeOffset UploadedAtUtc { get; private set; }

    private Document()
    {
    }

    public Document(
        Guid id,
        string originalFileName,
        string storageIdentifier,
        string contentType,
        long fileSize,
        DateTimeOffset uploadedAtUtc)
    {
        Id = id;
        OriginalFileName = originalFileName;
        StorageIdentifier = storageIdentifier;
        ContentType = contentType;
        FileSize = fileSize;
        UploadedAtUtc = uploadedAtUtc;
    }
}
