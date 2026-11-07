using System.Net.Http.Headers;
using DocumentManagement.Application.Documents;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagement.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(DocumentService documentService) : ControllerBase
{
    private const int MultipartOverheadAllowance = 64 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(DocumentUpload.MaxFileSizeBytes + MultipartOverheadAllowance)]
    [RequestFormLimits(MultipartBodyLengthLimit = DocumentUpload.MaxFileSizeBytes + MultipartOverheadAllowance)]
    [ProducesResponseType<DocumentDetails>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<DocumentDetails>> Upload(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            throw new DocumentValidationException("A file is required.");
        }

        var contentType = GetContentType(file.ContentType);
        using var content = file.OpenReadStream();
        var upload = new DocumentUpload(
            GetSafeFileName(file.FileName),
            contentType,
            file.Length,
            content);

        var details = await documentService.UploadAsync(upload, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = details.Id }, details);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DocumentDetails>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentDetails>>> List(
        CancellationToken cancellationToken) =>
        Ok(await documentService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DocumentDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDetails>> Get(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await documentService.GetAsync(id, cancellationToken));

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var download = await documentService.DownloadAsync(id, cancellationToken);
        return File(
            download.Content,
            download.Details.ContentType,
            download.Details.OriginalFileName);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await documentService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static string GetContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return "application/octet-stream";
        }

        if (!MediaTypeHeaderValue.TryParse(contentType, out var parsed))
        {
            throw new DocumentValidationException("The file content type is invalid.");
        }

        return parsed.ToString();
    }

    private static string GetSafeFileName(string? fileName)
    {
        var normalized = (fileName ?? string.Empty).Replace('\\', '/');
        var leafName = normalized[(normalized.LastIndexOf('/') + 1)..];
        var safeName = new string(leafName.Where(character => !char.IsControl(character)).ToArray()).Trim();

        return safeName;
    }
}
