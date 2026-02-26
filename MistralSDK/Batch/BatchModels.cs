using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.Batch
{
    /// <summary>
    /// Request to create a batch job.
    /// See <see href="https://docs.mistral.ai/api/endpoint/batch"/>.
    /// </summary>
    public class BatchJobCreateRequest
    {
        /// <summary>
        /// API endpoint for batch inference (e.g. /v1/chat/completions, /v1/embeddings, /v1/fim/completions).
        /// </summary>
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = ApiEndpoints.ChatCompletions;

        /// <summary>
        /// List of input file IDs (JSONL format). Alternative to Requests.
        /// </summary>
        [JsonPropertyName("input_files")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? InputFiles { get; set; }

        /// <summary>
        /// Inline requests. Alternative to InputFiles. Max 10000.
        /// </summary>
        [JsonPropertyName("requests")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BatchRequest>? Requests { get; set; }

        /// <summary>
        /// Model for batch inference.
        /// </summary>
        [JsonPropertyName("model")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Model { get; set; }

        /// <summary>
        /// Optional agent ID (deprecated agents API).
        /// </summary>
        [JsonPropertyName("agent_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AgentId { get; set; }

        /// <summary>
        /// Metadata to associate with the job.
        /// </summary>
        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Timeout in hours. Default 24.
        /// </summary>
        [JsonPropertyName("timeout_hours")]
        public int TimeoutHours { get; set; } = 24;
    }

    /// <summary>
    /// A single batch request.
    /// </summary>
    public class BatchRequest
    {
        [JsonPropertyName("custom_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CustomId { get; set; }

        /// <summary>
        /// Request body (e.g. { "max_tokens": 100, "messages": [...] } for chat).
        /// </summary>
        [JsonPropertyName("body")]
        public Dictionary<string, object> Body { get; set; } = new();

        /// <summary>
        /// Creates a batch request for chat completion.
        /// </summary>
        /// <param name="userMessage">The user message content.</param>
        /// <param name="maxTokens">Maximum tokens to generate. Default 100.</param>
        /// <param name="customId">Optional custom ID for tracking.</param>
        public static BatchRequest ForChat(string userMessage, int maxTokens = 100, string? customId = null) => new()
        {
            CustomId = customId,
            Body = new Dictionary<string, object>
            {
                ["max_tokens"] = maxTokens,
                ["messages"] = new[] { new { role = "user", content = userMessage } }
            }
        };
    }

    /// <summary>
    /// Batch job response.
    /// </summary>
    public class BatchJobResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = "batch";

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("input_files")]
        public List<string> InputFiles { get; set; } = new();

        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("agent_id")]
        public string? AgentId { get; set; }

        [JsonPropertyName("output_file")]
        public string? OutputFile { get; set; }

        [JsonPropertyName("error_file")]
        public string? ErrorFile { get; set; }

        [JsonPropertyName("errors")]
        public List<BatchError> Errors { get; set; } = new();

        [JsonPropertyName("outputs")]
        public List<Dictionary<string, object>>? Outputs { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("total_requests")]
        public int TotalRequests { get; set; }

        [JsonPropertyName("completed_requests")]
        public int CompletedRequests { get; set; }

        [JsonPropertyName("succeeded_requests")]
        public int SucceededRequests { get; set; }

        [JsonPropertyName("failed_requests")]
        public int FailedRequests { get; set; }

        [JsonPropertyName("started_at")]
        public long? StartedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public long? CompletedAt { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Indicates whether the job has reached a terminal state (success, failed, cancelled, or timeout).
        /// </summary>
        [JsonIgnore]
        public bool IsComplete => Status == BatchJobStatus.Success || Status == BatchJobStatus.Failed ||
            Status == BatchJobStatus.Cancelled || Status == BatchJobStatus.TimeoutExceeded;
    }

    /// <summary>
    /// Batch error.
    /// </summary>
    public class BatchError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; } = 1;
    }

    /// <summary>
    /// Batch jobs list response.
    /// </summary>
    public class BatchJobsListResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = "list";

        [JsonPropertyName("data")]
        public List<BatchJobResponse> Data { get; set; } = new();

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    /// <summary>
    /// API endpoint constants for batch.
    /// </summary>
    public static class ApiEndpoints
    {
        public const string ChatCompletions = "/v1/chat/completions";
        public const string Embeddings = "/v1/embeddings";
        public const string FimCompletions = "/v1/fim/completions";
        public const string Moderations = "/v1/moderations";
        public const string ChatModerations = "/v1/chat/moderations";
        public const string Ocr = "/v1/ocr";
        public const string Classifications = "/v1/classifications";
        public const string ChatClassifications = "/v1/chat/classifications";
    }

    /// <summary>
    /// Batch job status.
    /// </summary>
    public static class BatchJobStatus
    {
        public const string Queued = "QUEUED";
        public const string Running = "RUNNING";
        public const string Success = "SUCCESS";
        public const string Failed = "FAILED";
        public const string Cancelled = "CANCELLED";
        public const string CancellationRequested = "CANCELLATION_REQUESTED";
        public const string TimeoutExceeded = "TIMEOUT_EXCEEDED";
    }
}
