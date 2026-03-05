
namespace DrugStoreWebsiteAuthen.Application.Common;

public class Result
{
    public ResultStatus Status { get; set; }
    public bool Succeeded => Status == ResultStatus.Success || Status == ResultStatus.NoContent || Status == ResultStatus.Created;
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static Result Success(string? message = null)
        => new()
        {
            Status = ResultStatus.Success,
            Message = message ?? "Success"
        };

    public static Result Failure(ResultStatus status, params string[] errors)
        => new()
        {
            Status = status,
            Errors = errors.ToList(),
            Message = errors.FirstOrDefault() ?? "Error occurred"
        };
}

public class Result<T> : Result
{
    public T? Data { get; set; }

    public static Result<T> Success(ResultStatus status, T data, string? message = null)
        => new()
        {
            Status = status,
            Data = data,
            Message = message ?? "Success"
        };
    public static Result<T> Success(ResultStatus status, string? message = null)
        => new()
        {
            Status = status,
            Message = message ?? "Success"
        };

    public static new Result<T> Failure(ResultStatus status, params string[] errors)
        => new()
        {
            Status = status,
            Errors = errors.ToList(),
            Message = errors.FirstOrDefault() ?? "Error occurred"
        };
}
