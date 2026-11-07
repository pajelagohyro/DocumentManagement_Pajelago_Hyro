namespace DocumentManagement.Application.Documents;

public sealed class DocumentNotFoundException : Exception
{
    public DocumentNotFoundException(Guid id)
        : base($"Document '{id}' was not found.")
    {
    }
}

public sealed class DocumentValidationException : Exception
{
    public DocumentValidationException(string message)
        : base(message)
    {
    }
}

public sealed class DocumentTooLargeException : Exception
{
    public DocumentTooLargeException(long maxFileSizeBytes)
        : base($"The file exceeds the maximum allowed size of {maxFileSizeBytes} bytes.")
    {
    }
}
