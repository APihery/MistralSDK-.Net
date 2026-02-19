# Audio & Transcription

The Mistral SDK supports the [Audio & Transcription API](https://docs.mistral.ai/capabilities/audio_transcription) for transcribing audio files to text using Voxtral models.

## Overview

- **Transcription** – Convert audio (MP3, WAV, etc.) to text
- **Streaming** – Receive transcription events in real time
- **Diarization** – Identify who said what (multiple speakers)
- **Timestamps** – Get segment or word-level timing
- **Context bias** – Guide spelling of names and technical terms

## Models

| Constant | Description |
|----------|-------------|
| `AudioModels.VoxtralMiniLatest` | Voxtral Mini for transcription (recommended) |
| `AudioModels.VoxtralMini2507` | Voxtral Mini specific version |
| `AudioModels.VoxtralSmallLatest` | Voxtral Small for chat with audio |

## Audio input options

| Method | Use case |
|--------|----------|
| `AudioTranscriptionRequestBuilder.FromStream(stream, fileName)` | Upload audio from a stream |
| `AudioTranscriptionRequestBuilder.FromFileId(fileId)` | Use a file uploaded via Files API |
| `AudioTranscriptionRequestBuilder.FromFileUrl(url)` | Audio from a public URL |

## Basic transcription

### From a URL

```csharp
using MistralSDK;
using MistralSDK.Audio;

var client = new MistralClient(apiKey);

var request = AudioTranscriptionRequestBuilder.FromFileUrl(
    "https://example.com/recording.mp3");

var result = await client.AudioTranscribeAsync(request);

Console.WriteLine(result.Text);
Console.WriteLine($"Language: {result.Language}");
Console.WriteLine($"Duration: {result.Usage?.PromptAudioSeconds}s");
```

### From an uploaded file

```csharp
using MistralSDK;
using MistralSDK.Audio;
using MistralSDK.Files;

var client = new MistralClient(apiKey);

// Upload audio (use purpose "audio" for transcription)
using var stream = File.OpenRead("meeting.mp3");
var file = await client.FilesUploadAsync(stream, "meeting.mp3", FilePurpose.Audio);

// Transcribe
var request = AudioTranscriptionRequestBuilder.FromFileId(file.Id);
var result = await client.AudioTranscribeAsync(request);

Console.WriteLine(result.Text);

// Clean up
await client.FilesDeleteAsync(file.Id);
```

### From a local file stream

```csharp
using var stream = File.OpenRead("podcast.mp3");
var request = AudioTranscriptionRequestBuilder.FromStream(stream, "podcast.mp3");

var result = await client.AudioTranscribeAsync(request);
Console.WriteLine(result.Text);
```

## Advanced options

```csharp
var request = AudioTranscriptionRequestBuilder.FromFileUrl(
    "https://example.com/audio.mp3");

// Language hint (2-char code, e.g. "en", "fr")
request.Language = "en";

// Speaker diarization (who said what)
request.Diarize = true;

// Timestamps at segment or word level
request.TimestampGranularities = new List<string> 
{ 
    TimestampGranularity.Segment, 
    TimestampGranularity.Word 
};

// Context bias for names and technical terms (up to 100 words)
request.ContextBias = new List<string> 
{ 
    "Mistral AI", "voxtral", "Paris", "API" 
};

// Temperature (optional)
request.Temperature = 0.2;

var result = await client.AudioTranscribeAsync(request);

foreach (var segment in result.Segments)
{
    Console.WriteLine($"[{segment.Start:F1}s - {segment.End:F1}s] {segment.Text}");
    if (segment.SpeakerId != null)
        Console.WriteLine($"  Speaker: {segment.SpeakerId}");
}
```

## Streaming transcription

Receive transcription events as they arrive:

```csharp
var request = AudioTranscriptionRequestBuilder.FromFileUrl(
    "https://example.com/long-audio.mp3");

await foreach (var evt in client.AudioTranscribeStreamAsync(request))
{
    switch (evt)
    {
        case TranscriptionStreamTextDelta textDelta:
            Console.Write(textDelta.Text);
            break;
        case TranscriptionStreamLanguage lang:
            Console.WriteLine($"\nDetected language: {lang.AudioLanguage}");
            break;
        case TranscriptionStreamDone done:
            Console.WriteLine($"\n\nComplete. Model: {done.Model}");
            break;
    }
}
```

## Error handling

The API throws `MistralApiException` on errors:

```csharp
try
{
    var result = await client.AudioTranscribeAsync(request);
}
catch (MistralApiException ex)
{
    switch (ex.StatusCode)
    {
        case HttpStatusCode.BadRequest:
            // Invalid audio format, unsupported file type
            break;
        case HttpStatusCode.NotFound:
            // File not found (invalid file_id)
            break;
        case HttpStatusCode.Unauthorized:
            // Invalid API key
            break;
        case (HttpStatusCode)429:
            // Rate limit - use ex.RetryDelaySeconds
            break;
    }
}
```

## Security notes

- Validate file names before `FromStream` (no path traversal)
- Use HTTPS URLs only for `FromFileUrl`
- File URLs are limited to 2083 characters
- Do not expose API keys in client-side code

## See also

- [Mistral Audio & Transcription docs](https://docs.mistral.ai/capabilities/audio_transcription)
- [API reference](api-reference.md)
- [Files API](ocr.md#files-api) for uploading audio files
