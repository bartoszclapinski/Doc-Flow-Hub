namespace DocFlowHub.Core.Models.Common;

public class ServiceResult
{
    public bool Succeeded { get; private set; }
    public string? Error { get; private set; }

    protected ServiceResult(bool succeeded, string? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public static ServiceResult Success()
    {
        return new ServiceResult(true, null);
    }

    public static ServiceResult Failure(string error)
    {
        return new ServiceResult(false, error);
    }
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private set; }

    private ServiceResult(bool succeeded, string? error, T? data) 
        : base(succeeded, error)
    {
        Data = data;
    }

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>(true, null, data);
    }

    public new static ServiceResult<T> Failure(string error)
    {
        return new ServiceResult<T>(false, error, default);
    }
} 