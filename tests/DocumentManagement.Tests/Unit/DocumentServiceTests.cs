using DocumentManagement.Application.Documents;
using DocumentManagement.Domain.Models;

namespace DocumentManagement.Tests.Unit;

public sealed class DocumentServiceTests
{
    [Fact]
    public async Task UploadAsync_SavesFileAndReturnsMetadata()
    {
        var (service, repository, storage) = CreateService();
        var content = new MemoryStream([1, 2, 3]);

        var result = await service.UploadAsync(
            new DocumentUpload("report.pdf", "application/pdf", 3, content),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("report.pdf", result.OriginalFileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(3, result.FileSize);
        Assert.True(repository.Documents.ContainsKey(result.Id));
        Assert.Equal(new byte[] { 1, 2, 3 }, storage.Files[$"{result.Id:N}"]);
    }

    [Fact]
    public async Task GetAsync_ReturnsExistingDocumentMetadata()
    {
        var (service, repository, _) = CreateService();
        var document = CreateDocument();
        repository.Documents.Add(document.Id, document);

        var result = await service.GetAsync(document.Id, CancellationToken.None);

        Assert.Equal(document.Id, result.Id);
        Assert.Equal(document.OriginalFileName, result.OriginalFileName);
    }

    [Fact]
    public async Task GetAsync_ThrowsWhenDocumentDoesNotExist()
    {
        var (service, _, _) = CreateService();

        await Assert.ThrowsAsync<DocumentNotFoundException>(
            () => service.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_ReturnsDocumentMetadata()
    {
        var (service, repository, _) = CreateService();
        var document = CreateDocument();
        repository.Documents.Add(document.Id, document);

        var result = await service.ListAsync(CancellationToken.None);

        var listedDocument = Assert.Single(result);
        Assert.Equal(document.Id, listedDocument.Id);
        Assert.Equal(document.OriginalFileName, listedDocument.OriginalFileName);
        Assert.Equal(document.FileSize, listedDocument.FileSize);
    }

    [Fact]
    public async Task UploadAsync_RejectsFilesLargerThanTenMegabytes()
    {
        var (service, repository, storage) = CreateService();

        await Assert.ThrowsAsync<DocumentTooLargeException>(() =>
            service.UploadAsync(
                new DocumentUpload(
                    "large.bin",
                    "application/octet-stream",
                    DocumentUpload.MaxFileSizeBytes + 1,
                    Stream.Null),
                CancellationToken.None));

        Assert.Empty(repository.Documents);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task UploadAsync_RejectsInvalidMetadata()
    {
        var (service, repository, storage) = CreateService();

        await Assert.ThrowsAsync<DocumentValidationException>(() =>
            service.UploadAsync(
                new DocumentUpload("", "text/plain", 3, new MemoryStream([1, 2, 3])),
                CancellationToken.None));

        Assert.Empty(repository.Documents);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task UploadAsync_RejectsEmptyFiles()
    {
        var (service, repository, storage) = CreateService();

        await Assert.ThrowsAsync<DocumentValidationException>(() =>
            service.UploadAsync(
                new DocumentUpload("empty.txt", "text/plain", 0, Stream.Null),
                CancellationToken.None));

        Assert.Empty(repository.Documents);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task DeleteAsync_RemovesMetadataAndStoredFile()
    {
        var (service, repository, storage) = CreateService();
        var document = CreateDocument();
        repository.Documents.Add(document.Id, document);
        storage.Files.Add(document.StorageIdentifier, [1, 2]);

        await service.DeleteAsync(document.Id, CancellationToken.None);

        Assert.Empty(repository.Documents);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenDocumentDoesNotExist()
    {
        var (service, _, _) = CreateService();

        await Assert.ThrowsAsync<DocumentNotFoundException>(
            () => service.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_ReturnsFileContentAndMetadata()
    {
        var (service, repository, storage) = CreateService();
        var document = CreateDocument();
        var fileContent = new byte[] { 1, 2, 3 };
        repository.Documents.Add(document.Id, document);
        storage.Files.Add(document.StorageIdentifier, fileContent);

        var result = await service.DownloadAsync(document.Id, CancellationToken.None);
        using var content = new MemoryStream();
        await result.Content.CopyToAsync(content);

        Assert.Equal(document.Id, result.Details.Id);
        Assert.Equal(fileContent, content.ToArray());
    }

    [Fact]
    public async Task DownloadAsync_ThrowsWhenStoredFileIsMissing()
    {
        var (service, repository, _) = CreateService();
        var document = CreateDocument();
        repository.Documents.Add(document.Id, document);

        await Assert.ThrowsAsync<DocumentNotFoundException>(
            () => service.DownloadAsync(document.Id, CancellationToken.None));
    }

    private static (DocumentService Service, TestDocumentRepository Repository, TestDocumentStorage Storage)
        CreateService()
    {
        var repository = new TestDocumentRepository();
        var storage = new TestDocumentStorage();
        return (new DocumentService(repository, storage), repository, storage);
    }

    private static Document CreateDocument()
    {
        var id = Guid.NewGuid();
        return new Document(
            id,
            "existing.txt",
            id.ToString("N"),
            "text/plain",
            4,
            DateTimeOffset.UtcNow);
    }

    private sealed class TestDocumentRepository : IDocumentRepository
    {
        public Dictionary<Guid, Document> Documents { get; } = [];

        public Task AddAsync(Document document, CancellationToken cancellationToken)
        {
            Documents.Add(document.Id, document);
            return Task.CompletedTask;
        }

        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Documents.TryGetValue(id, out var document);
            return Task.FromResult(document);
        }

        public Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Document>>(Documents.Values.ToArray());

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Documents.Remove(id));
    }

    private sealed class TestDocumentStorage : IDocumentStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];
        public async Task StoreAsync(
            string storageIdentifier,
            Stream content,
            CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Files.Add(storageIdentifier, buffer.ToArray());
        }

        public Task<Stream?> OpenReadAsync(string storageIdentifier, CancellationToken cancellationToken) =>
            Task.FromResult<Stream?>(Files.TryGetValue(storageIdentifier, out var bytes)
                ? new MemoryStream(bytes, writable: false)
                : null);

        public Task DeleteAsync(string storageIdentifier, CancellationToken cancellationToken)
        {
            Files.Remove(storageIdentifier);
            return Task.CompletedTask;
        }
    }
}
