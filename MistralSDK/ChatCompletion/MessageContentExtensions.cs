using System.Collections.Generic;

namespace MistralSDK.ChatCompletion
{
    /// <summary>
    /// Extension methods for extracting text from message content (string or content chunks).
    /// </summary>
    public static class MessageContentExtensions
    {
        /// <summary>
        /// Extracts the display text from content (string or chunks).
        /// For reasoning responses, returns the final answer text (skips thinking chunks).
        /// </summary>
        public static string? GetContentText(object? content)
        {
            if (content == null) return null;
            if (content is string s) return s;
            if (content is List<ContentChunk> chunks)
                return ContentChunkBuilder.ExtractAnswerText(chunks);
            return null;
        }

        /// <summary>
        /// Extracts all text including reasoning traces.
        /// </summary>
        public static string? GetAllContentText(object? content)
        {
            if (content == null) return null;
            if (content is string s) return s;
            if (content is List<ContentChunk> chunks)
                return ContentChunkBuilder.ExtractAllText(chunks);
            return null;
        }

        /// <summary>
        /// Extracts only the thinking/reasoning text from content chunks.
        /// Returns null if content is a simple string.
        /// </summary>
        public static string? GetThinkingText(object? content)
        {
            if (content is not List<ContentChunk> chunks) return null;
            return ContentChunkBuilder.ExtractThinkingText(chunks);
        }

        /// <summary>
        /// Gets content as chunks if structured; otherwise null.
        /// </summary>
        public static List<ContentChunk>? GetContentChunks(object? content)
        {
            return content as List<ContentChunk>;
        }
    }
}
