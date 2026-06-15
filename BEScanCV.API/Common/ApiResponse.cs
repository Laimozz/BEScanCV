namespace BEScanCV.API.Common;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string Message { get; set; } = "OK";
    public bool Success { get; set; } = true;
    public int StatusCode { get; set; } = 200;

    public ApiResponse(T? data)
    {
        Data = data;
    }
}