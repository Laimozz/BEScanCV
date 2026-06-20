namespace BEScanCV.Application.Exceptions;

public sealed class CvUploadValidationException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

