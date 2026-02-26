# Fine-Tuning API

The Mistral SDK supports the [Fine-Tuning API](https://docs.mistral.ai/api/endpoint/fine-tuning) for training custom models on your data. Create jobs, monitor progress, and use the resulting fine-tuned models.

## Overview

- **Endpoints** – Create, list, get, cancel, and start fine-tuning jobs
- **Job types** – Completion (default) and classifier
- **Base models** – open-mistral-7b, mistral-small-latest, codestral-latest, etc.

## Create a fine-tuning job

### 1. Prepare training data

Upload a JSONL file with purpose `fine-tune`. Format depends on job type (completion vs classifier). See [Mistral fine-tuning docs](https://docs.mistral.ai/capabilities/finetuning/) for data format.

```csharp
using MistralSDK;
using MistralSDK.Files;
using MistralSDK.FineTuning;

var client = new MistralClient(apiKey);

using var fileStream = File.OpenRead("training.jsonl");
var fileInfo = await client.FilesUploadAsync(fileStream, "training.jsonl", FilePurposeType.FineTune);
```

### 2. Create the job

```csharp
var request = new FineTuningJobCreateRequest
{
    Model = FineTuneableModels.OpenMistral7B,
    TrainingFiles = new List<TrainingFile> { TrainingFile.From(fileInfo.Id) },
    Hyperparameters = new CompletionTrainingParameters
    {
        TrainingSteps = 100,
        LearningRate = 0.0001,
        WeightDecay = 0.1,
        WarmupFraction = 0.05
    },
    Suffix = "my-custom-model",
    AutoStart = true
};

var job = await client.FineTuningJobCreateAsync(request);
Console.WriteLine($"Job ID: {job.Id}, Status: {job.Status}");
```

### With validation files

```csharp
var validationFile = await client.FilesUploadAsync(validationStream, "validation.jsonl", FilePurposeType.FineTune);

var request = new FineTuningJobCreateRequest
{
    Model = FineTuneableModels.MistralSmall,
    TrainingFiles = new List<TrainingFile> { TrainingFile.From(fileInfo.Id) },
    ValidationFiles = new List<string> { validationFile.Id },
    Hyperparameters = new CompletionTrainingParameters { TrainingSteps = 50 },
    AutoStart = true
};
```

## List fine-tuning jobs

```csharp
var response = await client.FineTuningJobsListAsync(limit: 20, after: null);

foreach (var job in response.Data)
{
    Console.WriteLine($"{job.Id}: {job.Status} - {job.Model}");
}
```

## Get job status

```csharp
var job = await client.FineTuningJobGetAsync(jobId);

if (job.IsComplete && job.Status == FineTuningJobStatus.Success)
{
    Console.WriteLine($"Fine-tuned model: {job.FineTunedModel}");
}
```

## Cancel a job

```csharp
var job = await client.FineTuningJobCancelAsync(jobId);
```

## List all jobs (paginated)

```csharp
var allJobs = await client.FineTuningJobsListAllAsync();
```

## Wait until complete

```csharp
var job = await client.FineTuningJobWaitUntilCompleteAsync(jobId, pollIntervalMs: 5000, timeoutMs: 86400000);
if (job.IsComplete && job.Status == FineTuningJobStatus.Success)
    Console.WriteLine($"Fine-tuned model: {job.FineTunedModel}");
```

## Start a job (when auto_start=false)

```csharp
var request = new FineTuningJobCreateRequest
{
    Model = FineTuneableModels.OpenMistral7B,
    TrainingFiles = new List<TrainingFile> { TrainingFile.From(fileInfo.Id) },
    Hyperparameters = new CompletionTrainingParameters { TrainingSteps = 100 },
    AutoStart = false
};

var job = await client.FineTuningJobCreateAsync(request);

// Later, start the job
job = await client.FineTuningJobStartAsync(job.Id);
```

## Hyperparameters (Completion)

| Property         | Type   | Description                          |
|------------------|--------|--------------------------------------|
| `TrainingSteps`  | int    | Number of training steps             |
| `LearningRate`   | double | Default 0.0001                       |
| `WeightDecay`    | double | Optional, default 0.1                |
| `WarmupFraction` | double | Optional, default 0.05               |
| `Epochs`         | double | Optional                             |
| `SeqLen`         | int    | Optional sequence length              |
| `FimRatio`       | double | Optional FIM ratio                   |

## Fine-tuneable models

```csharp
FineTuneableModels.Ministral3B      // ministral-3b-latest
FineTuneableModels.Ministral8B      // ministral-8b-latest
FineTuneableModels.OpenMistral7B    // open-mistral-7b
FineTuneableModels.OpenMistralNemo  // open-mistral-nemo
FineTuneableModels.MistralSmall     // mistral-small-latest
FineTuneableModels.MistralMedium    // mistral-medium-latest
FineTuneableModels.MistralLarge     // mistral-large-latest
FineTuneableModels.Pixtral12B       // pixtral-12b-latest
FineTuneableModels.Codestral        // codestral-latest
```

## Job status values

- `QUEUED` – Waiting to start
- `STARTED` – Job started
- `VALIDATING` – Validating data
- `VALIDATED` – Validation passed
- `RUNNING` – Training in progress
- `FAILED_VALIDATION` – Validation failed
- `FAILED` – Training failed
- `SUCCESS` – Completed successfully
- `CANCELLED` – Cancelled
- `CANCELLATION_REQUESTED` – Cancel requested
