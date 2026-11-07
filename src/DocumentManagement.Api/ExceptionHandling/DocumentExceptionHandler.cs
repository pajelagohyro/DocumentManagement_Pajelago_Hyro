using DocumentManagement.Application.Documents;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagement.Api.ExceptionHandling;

public sealed class DocumentExceptionHandler(
    ILogger<DocumentExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            DocumentNotFoundException => (
                StatusCodes.Status404NotFound,
                "Document not found",
                exception.Message),
            DocumentTooLargeException => (
                StatusCodes.Status413PayloadTooLarge,
                "File too large",
                exception.Message),
            BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge } => (
                StatusCodes.Status413PayloadTooLarge,
                "Request too large",
                "The request exceeds the maximum allowed size."),
            DocumentValidationException => (
                StatusCodes.Status400BadRequest,
                "Invalid document",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "The request could not be completed.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "An unhandled error occurred while processing a document request.");
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            },
            cancellationToken);

        return true;
    }
}
