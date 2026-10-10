namespace ProductsApi.Models;

public class ReturnResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Result { get; init; }
    public string[] ErrorMessage { get; init; } = [];

    public static ReturnResult<T> Success(T result) => new()
    {
        IsSuccess = true,
        Result = result
    };

    public static ReturnResult<T> Failure(params string[] errors) => new()
    {
        IsSuccess = false,
        ErrorMessage = errors
    };
}
