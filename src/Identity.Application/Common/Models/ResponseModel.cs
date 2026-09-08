namespace Identity.Application.Common.Models;

public class ResponseModel<T>
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ResponseModel<T> Success(T data, string? message = null)
    {
        return new ResponseModel<T>
        {
            Succeeded = true,
            Message = message,
            Data = data
        };
    }

    public static ResponseModel<T> Failure(string message, List<string>? errors = null)
    {
        return new ResponseModel<T>
        {
            Succeeded = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}
