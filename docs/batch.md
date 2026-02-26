# Batch API

The Mistral SDK supports the [Batch API](https://docs.mistral.ai/api/endpoint/batch) for asynchronous batch inference. Submit many requests in a single job and retrieve results when processing is complete.

## Overview

- **Endpoints** – Create, list, get, and cancel batch jobs
- **Supported APIs** – Chat completions, embeddings, FIM, moderations, classifications, OCR
- **Input** – JSONL file(s) or inline requests (max 10,000)

## Create a batch job

### With input files (JSONL)

Upload a JSONL file where each line is a request body for the chosen endpoint:

```csharp
using MistralSDK;
using MistralSDK.Batch;
using MistralSDK.Files;  // FilePurposeType.Batch, FilePurpose.Batch, etc.

var client = new MistralClient(apiKey);

// 1. Upload JSONL file with purpose "batch" (use FilePurposeType for type-safe overload)
using var fileStream = File.OpenRead("requests.jsonl");
var fileInfo = await client.FilesUploadAsync(fileStream, "requests.jsonl", FilePurposeType.Batch);

// 2. Create batch job
var request = new BatchJobCreateRequest
{
    Endpoint = ApiEndpoints.ChatCompletions,
    InputFiles = new List<string> { fileInfo.Id },
    Model = "mistral-small-latest",
    TimeoutHours = 24
};

var job = await client.BatchJobCreateAsync(request);
Console.WriteLine($"Job ID: {job.Id}, Status: {job.Status}");
```

### With inline requests

```csharp
// Using BatchRequest.ForChat for chat completions
var request = new BatchJobCreateRequest
{
    Endpoint = ApiEndpoints.ChatCompletions,
    Model = "mistral-small-latest",
    Requests = new List<BatchRequest>
    {
        BatchRequest.ForChat("Hello", maxTokens: 100, customId: "req-1"),
        BatchRequest.ForChat("World", maxTokens: 100, customId: "req-2")
    }
};

var job = await client.BatchJobCreateAsync(request);
```

## List batch jobs

```csharp
var response = await client.BatchJobsListAsync(limit: 20, after: null);

foreach (var job in response.Data)
{
    Console.WriteLine($"{job.Id}: {job.Status} - {job.CompletedRequests}/{job.TotalRequests}");
}
```

## Get job status

```csharp
var job = await client.BatchJobGetAsync(jobId);

if (job.IsComplete && job.Status == BatchJobStatus.Success)
{
    // Download output file if available
    if (!string.IsNullOrEmpty(job.OutputFile))
    {
        // Use FilesDownloadAsync with job.OutputFile or a signed URL
    }
}
```

## Cancel a job

```csharp
var job = await client.BatchJobCancelAsync(jobId);
Console.WriteLine($"Status after cancel: {job.Status}");
```

## List all jobs (paginated)

```csharp
var allJobs = await client.BatchJobsListAllAsync();
```

## Wait until complete

```csharp
var job = await client.BatchJobWaitUntilCompleteAsync(jobId, pollIntervalMs: 5000, timeoutMs: 3600000);
if (job.IsComplete && job.Status == BatchJobStatus.Success)
    Console.WriteLine("Done!");
```

## Supported endpoints

| Constant                 | Endpoint                    |
|--------------------------|-----------------------------|
| `ApiEndpoints.ChatCompletions` | `/v1/chat/completions`      |
| `ApiEndpoints.Embeddings`      | `/v1/embeddings`            |
| `ApiEndpoints.FimCompletions`  | `/v1/fim/completions`       |
| `ApiEndpoints.Moderations`     | `/v1/moderations`           |
| `ApiEndpoints.ChatModerations` | `/v1/chat/moderations`      |
| `ApiEndpoints.Classifications`| `/v1/classifications`       |
| `ApiEndpoints.ChatClassifications` | `/v1/chat/classifications` |
| `ApiEndpoints.Ocr`             | `/v1/ocr`                   |

## Job status values

- `QUEUED` – Waiting to start
- `RUNNING` – Processing
- `SUCCESS` – Completed successfully
- `FAILED` – Failed
- `CANCELLED` – Cancelled
- `CANCELLATION_REQUESTED` – Cancel requested
- `TIMEOUT_EXCEEDED` – Exceeded timeout
