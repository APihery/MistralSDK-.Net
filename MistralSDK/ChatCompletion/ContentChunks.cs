using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.ChatCompletion
{
    /// <summary>
    /// Base type for content chunks in messages (request and response).
    /// Used for reasoning models where content can be text or thinking traces.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(TextChunk), "text")]
    [JsonDerivedType(typeof(ThinkChunk), "thinking")]
    public abstract class ContentChunk
    {
        /// <summary>Chunk type discriminator.</summary>
        [JsonPropertyName("type")]
        public abstract string Type { get; }
    }

    /// <summary>
    /// A text content chunk.
    /// </summary>
    public class TextChunk : ContentChunk
    {
        [JsonPropertyName("type")]
        public override string Type => "text";

        /// <summary>The text content.</summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// A thinking/reasoning chunk. Contains the model's reasoning traces.
    /// </summary>
    public class ThinkChunk : ContentChunk
    {
        [JsonPropertyName("type")]
        public override string Type => "thinking";

        /// <summary>Whether the thinking chunk is closed. Used for prefixing.</summary>
        [JsonPropertyName("closed")]
        public bool Closed { get; set; } = true;

        /// <summary>Nested chunks (typically TextChunk) with reasoning traces.</summary>
        [JsonPropertyName("thinking")]
        public List<ContentChunk> Thinking { get; set; } = new List<ContentChunk>();
    }

    /// <summary>
    /// Helper to build content chunks for reasoning messages.
    /// </summary>
    public static class ContentChunkBuilder
    {
        /// <summary>Creates a text chunk.</summary>
        public static TextChunk Text(string text) => new TextChunk { Text = text ?? string.Empty };

        /// <summary>Creates a thinking chunk with inner text.</summary>
        public static ThinkChunk Thinking(string text, bool closed = true) =>
            new ThinkChunk { Thinking = new List<ContentChunk> { new TextChunk { Text = text ?? string.Empty } }, Closed = closed };

        /// <summary>Creates a thinking chunk with inner chunks.</summary>
        public static ThinkChunk Thinking(List<ContentChunk> chunks, bool closed = true) =>
            new ThinkChunk { Thinking = chunks ?? new List<ContentChunk>(), Closed = closed };

        /// <summary>
        /// Extracts all text from content chunks (text chunks and text inside thinking).
        /// </summary>
        public static string ExtractAllText(IEnumerable<ContentChunk>? chunks)
        {
            if (chunks == null) return string.Empty;
            var sb = new System.Text.StringBuilder();
            foreach (var c in chunks)
            {
                if (c == null) continue;
                if (c is TextChunk tc)
                    sb.Append(tc?.Text);
                else if (c is ThinkChunk thc)
                    sb.Append(ExtractAllText(thc?.Thinking));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Extracts only the final answer text (skips thinking chunks).
        /// </summary>
        public static string ExtractAnswerText(IEnumerable<ContentChunk>? chunks)
        {
            if (chunks == null) return string.Empty;
            var sb = new System.Text.StringBuilder();
            foreach (var c in chunks)
            {
                if (c is TextChunk tc)
                    sb.Append(tc?.Text);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Extracts only the thinking/reasoning text.
        /// </summary>
        public static string ExtractThinkingText(IEnumerable<ContentChunk>? chunks)
        {
            if (chunks == null) return string.Empty;
            var sb = new System.Text.StringBuilder();
            foreach (var c in chunks)
            {
                if (c is ThinkChunk thc)
                    sb.Append(ExtractAllText(thc?.Thinking));
            }
            return sb.ToString();
        }
    }
}
