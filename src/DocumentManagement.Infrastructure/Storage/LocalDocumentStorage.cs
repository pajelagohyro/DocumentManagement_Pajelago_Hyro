using DocumentManagement.Application.Documents;
using Microsoft.Extensions.Options;

namespace DocumentManagement.Infrastructure.Storage;

public sealed class LocalDocumentStorage(
    IOptions<DocumentStorageOptions> options) : IDocumentStorage
{
    private readonly string _rootPath = options.Value.RootPath;

    public async Task StoreAsync(
        string storageIdentifier,
        Stream content,
        CancellationToken cancellationToken)
    {
        var path = GetPath(storageIdentifier);
        Directory.CreateDirectory(_rootPath);

        var fileCreated = false;
        try
        {
            await using var destination = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            fileCreated = true;
            await content.CopyToAsync(destination, cancellationToken);
        }
        catch (Exception storageException)
        {
            if (!fileCreated)
            {
                throw;
            }

            try
            {
                File.Delete(path);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "Storing the document failed and the partial file could not be removed.",
                    storageException,
                    cleanupException);
            }

            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string storageIdentifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetPath(storageIdentifier);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storageIdentifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        File.Delete(GetPath(storageIdentifier));
        return Task.CompletedTask;
    }

    private string GetPath(string storageIdentifier) =>
        Path.Combine(_rootPath, $"{ValidateIdentifier(storageIdentifier)}.document");

    private static string ValidateIdentifier(string storageIdentifier) =>
        Guid.TryParseExact(storageIdentifier, "N", out var id)
            ? id.ToString("N")
            : throw new ArgumentException("The storage identifier is invalid.", nameof(storageIdentifier));
}
