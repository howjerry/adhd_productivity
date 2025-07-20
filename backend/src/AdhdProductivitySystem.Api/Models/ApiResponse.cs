using System.Text.Json.Serialization;

namespace AdhdProductivitySystem.Api.Models;

/// <summary>
/// 統一 API 回應格式的基礎類別
/// </summary>
/// <typeparam name="T">回應資料類型</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 回應資料（成功時才有值）
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    /// <summary>
    /// 錯誤資訊（失敗時才有值）
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiError? Error { get; set; }

    /// <summary>
    /// 額外的元資訊
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiMeta? Meta { get; set; }

    /// <summary>
    /// 回應時間戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 創建成功回應
    /// </summary>
    public static ApiResponse<T> CreateSuccess(T data, ApiMeta? meta = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Meta = meta
        };
    }

    /// <summary>
    /// 創建錯誤回應
    /// </summary>
    public static ApiResponse<T> CreateError(string errorCode, string message, string? errorId = null, object? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ApiError
            {
                Code = errorCode,
                Message = message,
                ErrorId = errorId,
                Details = details
            }
        };
    }

    /// <summary>
    /// 創建錯誤回應
    /// </summary>
    public static ApiResponse<T> CreateError(ApiError error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error
        };
    }
}

/// <summary>
/// 無資料的 API 回應格式
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// 創建成功回應（無資料）
    /// </summary>
    public static ApiResponse CreateSuccess(string? message = null, ApiMeta? meta = null)
    {
        return new ApiResponse
        {
            Success = true,
            Meta = meta
        };
    }

    /// <summary>
    /// 創建錯誤回應
    /// </summary>
    public new static ApiResponse CreateError(string errorCode, string message, string? errorId = null, object? details = null)
    {
        return new ApiResponse
        {
            Success = false,
            Error = new ApiError
            {
                Code = errorCode,
                Message = message,
                ErrorId = errorId,
                Details = details
            }
        };
    }
}

/// <summary>
/// API 錯誤資訊
/// </summary>
public class ApiError
{
    /// <summary>
    /// 錯誤代碼
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 錯誤追蹤 ID（可選）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorId { get; set; }

    /// <summary>
    /// 詳細錯誤資訊（開發環境可用）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Details { get; set; }

    /// <summary>
    /// 驗證錯誤的詳細資訊
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}

/// <summary>
/// API 元資訊
/// </summary>
public class ApiMeta
{
    /// <summary>
    /// 分頁資訊
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMeta? Pagination { get; set; }

    /// <summary>
    /// 快取資訊
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CacheMeta? Cache { get; set; }

    /// <summary>
    /// 自訂額外資訊
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Additional { get; set; }
}

/// <summary>
/// 分頁元資訊
/// </summary>
public class PaginationMeta
{
    /// <summary>
    /// 當前頁碼
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// 每頁筆數
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 總筆數
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 總頁數
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// 是否有上一頁
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// 是否有下一頁
    /// </summary>
    public bool HasNext { get; set; }
}

/// <summary>
/// 快取元資訊
/// </summary>
public class CacheMeta
{
    /// <summary>
    /// 是否來自快取
    /// </summary>
    public bool IsFromCache { get; set; }

    /// <summary>
    /// 快取過期時間
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 快取 TTL（秒）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TtlSeconds { get; set; }
}