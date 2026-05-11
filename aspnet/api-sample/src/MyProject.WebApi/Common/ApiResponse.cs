using System.Collections.ObjectModel;

namespace MyProject.WebApi.Common;

/// <summary>
///     Standard API response wrapper
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }

    public static ApiResponse Ok(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    public static ApiResponse Fail(string? message = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message
        };
    }
}

/// <summary>
///     Standard API response wrapper for successful responses with data
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public new static ApiResponse<T> Fail(string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message
        };
    }
}

/// <summary>
///     Error API response with detailed error information
/// </summary>
public class ErrorApiResponse : ApiResponse
{
    public IReadOnlyList<string> Errors { get; set; } = [];

    public static ErrorApiResponse Create(string message, IEnumerable<string>? errors = null)
    {
        return new ErrorApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors?.ToList().AsReadOnly() ?? new ReadOnlyCollection<string>(Array.Empty<string>())
        };
    }
}