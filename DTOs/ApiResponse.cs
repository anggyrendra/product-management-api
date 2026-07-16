using System.Text.Json.Serialization;

namespace ProductApi.DTOs;

/// <summary>
/// Generic envelope used for paginated list responses.
/// Provides metadata about the page along with the items.
/// </summary>
public class PaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("has_next")]
    public bool HasNext => Page < TotalPages;

    [JsonPropertyName("has_prev")]
    public bool HasPrev => Page > 1;
}

/// <summary>
/// Standard success envelope used for single-object responses.
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

/// <summary>
/// Standard error envelope. Contains a message and optional
/// list of validation errors keyed by field name.
/// </summary>
public class ApiErrorResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = false;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("errors")]
    public Dictionary<string, List<string>>? Errors { get; set; }
}
