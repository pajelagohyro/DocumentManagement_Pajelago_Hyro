using DocumentManagement.Application.Documents;
using DocumentManagement.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagement.Infrastructure.Data;

public sealed class DocumentRepository(DocumentDbContext dbContext) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Documents.SingleOrDefaultAsync(document => document.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Documents
            .AsNoTracking()
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Documents
            .Where(document => document.Id == id)
            .ExecuteDeleteAsync(cancellationToken) > 0;
}
