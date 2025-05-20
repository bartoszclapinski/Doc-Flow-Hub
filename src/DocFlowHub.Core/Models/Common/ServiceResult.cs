namespace DocFlowHub.Core.Models.Common;

public class ServiceResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }

    public static ServiceResult Success()
    {
        return new ServiceResult { Succeeded = true, Error = null };
    }

    public static ServiceResult Failure(string error)
    {
        return new ServiceResult { Succeeded = false, Error = error };
    }
} 