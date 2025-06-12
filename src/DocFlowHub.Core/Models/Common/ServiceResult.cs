namespace DocFlowHub.Core.Models.Common;

public class ServiceResult
{
    public bool Succeeded { get; private set; }
    public string? Error { get; private set; }

    protected ServiceResult(bool succeeded, string? error = null)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public static ServiceResult Success() => new(true);
    public static ServiceResult Failure(string error) => new(false, error);
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private set; }

    private ServiceResult(bool succeeded, T? data = default, string? error = null)
        : base(succeeded, error)
    {
        Data = data;
    }

    public static ServiceResult<T> Success(T data) => new(true, data);
    public new static ServiceResult<T> Failure(string error) => new(false, error: error);
} 