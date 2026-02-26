using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.FineTuning
{
    /// <summary>
    /// Request to create a fine-tuning job.
    /// See <see href="https://docs.mistral.ai/api/endpoint/fine-tuning"/>.
    /// </summary>
    public class FineTuningJobCreateRequest
    {
        /// <summary>
        /// Base model to fine-tune (e.g. open-mistral-7b, mistral-small-latest).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Training files with optional weights.
        /// </summary>
        [JsonPropertyName("training_files")]
        public List<TrainingFile> TrainingFiles { get; set; } = new();

        /// <summary>
        /// Optional validation file IDs.
        /// </summary>
        [JsonPropertyName("validation_files")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? ValidationFiles { get; set; }

        /// <summary>
        /// Hyperparameters for training.
        /// </summary>
        [JsonPropertyName("hyperparameters")]
        public CompletionTrainingParameters Hyperparameters { get; set; } = new();

        /// <summary>
        /// Suffix for the fine-tuned model name (max 18 chars).
        /// </summary>
        [JsonPropertyName("suffix")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Suffix { get; set; }

        /// <summary>
        /// Whether to auto-start the job. Default true.
        /// </summary>
        [JsonPropertyName("auto_start")]
        public bool AutoStart { get; set; } = true;

        /// <summary>
        /// Job type: completion or classifier.
        /// </summary>
        [JsonPropertyName("job_type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? JobType { get; set; }
    }

    /// <summary>
    /// Training file with optional weight.
    /// </summary>
    public class TrainingFile
    {
        [JsonPropertyName("file_id")]
        public string FileId { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public double Weight { get; set; } = 1.0;

        /// <summary>
        /// Creates a training file reference.
        /// </summary>
        /// <param name="fileId">The file ID from Files API.</param>
        /// <param name="weight">Weight for the file. Default 1.0.</param>
        public static TrainingFile From(string fileId, double weight = 1.0) => new() { FileId = fileId, Weight = weight };
    }

    /// <summary>
    /// Completion fine-tuning hyperparameters.
    /// </summary>
    public class CompletionTrainingParameters
    {
        [JsonPropertyName("training_steps")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TrainingSteps { get; set; }

        [JsonPropertyName("learning_rate")]
        public double LearningRate { get; set; } = 0.0001;

        [JsonPropertyName("weight_decay")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? WeightDecay { get; set; }

        [JsonPropertyName("warmup_fraction")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? WarmupFraction { get; set; }

        [JsonPropertyName("epochs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Epochs { get; set; }

        [JsonPropertyName("seq_len")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SeqLen { get; set; }

        [JsonPropertyName("fim_ratio")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? FimRatio { get; set; }
    }

    /// <summary>
    /// Fine-tuning job response.
    /// </summary>
    public class FineTuningJobResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = "job";

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("modified_at")]
        public long ModifiedAt { get; set; }

        [JsonPropertyName("training_files")]
        public List<string> TrainingFiles { get; set; } = new();

        [JsonPropertyName("validation_files")]
        public List<string>? ValidationFiles { get; set; }

        [JsonPropertyName("fine_tuned_model")]
        public string? FineTunedModel { get; set; }

        [JsonPropertyName("suffix")]
        public string? Suffix { get; set; }

        [JsonPropertyName("auto_start")]
        public bool AutoStart { get; set; }

        [JsonPropertyName("job_type")]
        public string JobType { get; set; } = "completion";

        [JsonPropertyName("hyperparameters")]
        public object? Hyperparameters { get; set; }

        [JsonPropertyName("trained_tokens")]
        public int? TrainedTokens { get; set; }

        /// <summary>
        /// Indicates whether the job has reached a terminal state (success, failed, or cancelled).
        /// </summary>
        [JsonIgnore]
        public bool IsComplete => Status == FineTuningJobStatus.Success || Status == FineTuningJobStatus.Failed ||
            Status == FineTuningJobStatus.Cancelled || Status == FineTuningJobStatus.CancellationRequested;
    }

    /// <summary>
    /// Fine-tuning jobs list response.
    /// </summary>
    public class FineTuningJobsListResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = "list";

        [JsonPropertyName("data")]
        public List<FineTuningJobResponse> Data { get; set; } = new();

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    /// <summary>
    /// Fine-tuneable model constants.
    /// </summary>
    public static class FineTuneableModels
    {
        public const string Ministral3B = "ministral-3b-latest";
        public const string Ministral8B = "ministral-8b-latest";
        public const string OpenMistral7B = "open-mistral-7b";
        public const string OpenMistralNemo = "open-mistral-nemo";
        public const string MistralSmall = "mistral-small-latest";
        public const string MistralMedium = "mistral-medium-latest";
        public const string MistralLarge = "mistral-large-latest";
        public const string Pixtral12B = "pixtral-12b-latest";
        public const string Codestral = "codestral-latest";
    }

    /// <summary>
    /// Fine-tuning job status.
    /// </summary>
    public static class FineTuningJobStatus
    {
        public const string Queued = "QUEUED";
        public const string Started = "STARTED";
        public const string Validating = "VALIDATING";
        public const string Validated = "VALIDATED";
        public const string Running = "RUNNING";
        public const string FailedValidation = "FAILED_VALIDATION";
        public const string Failed = "FAILED";
        public const string Success = "SUCCESS";
        public const string Cancelled = "CANCELLED";
        public const string CancellationRequested = "CANCELLATION_REQUESTED";
    }
}
