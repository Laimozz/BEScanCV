namespace BEScanCV.Application.Exceptions;

public class AiParserException : Exception
{
    public int StatusCode { get; }
    public string ResponseBody { get; }

    public AiParserException(int statusCode, string responseBody) 
        : base(responseBody)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}