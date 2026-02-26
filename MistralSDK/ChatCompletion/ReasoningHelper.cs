using System;
using System.Collections.Generic;

namespace MistralSDK.ChatCompletion
{
    /// <summary>
    /// Helpers for reasoning models (Magistral).
    /// </summary>
    public static class ReasoningHelper
    {
        /// <summary>
        /// Default system prompt for reasoning models (recommended by Mistral).
        /// Instructs the model to draft thinking before the final answer.
        /// </summary>
        public static List<ContentChunk> DefaultReasoningSystemPrompt()
        {
            return new List<ContentChunk>
            {
                ContentChunkBuilder.Text(
                    "# HOW YOU SHOULD THINK AND ANSWER\n\n" +
                    "First draft your thinking process (inner monologue) until you arrive at a response. " +
                    "Format your response using Markdown, and use LaTeX for any mathematical equations. " +
                    "Write both your thoughts and the response in the same language as the input.\n\n" +
                    "Your thinking process must follow the template below:"),
                ContentChunkBuilder.Thinking(
                    "Your thoughts or/and draft, like working through an exercise on scratch paper. " +
                    "Be as casual and as long as you want until you are confident to generate the response to the user."),
                ContentChunkBuilder.Text("Here, provide a self-contained response.")
            };
        }

        /// <summary>
        /// Creates a chat completion request for reasoning with the default system prompt.
        /// </summary>
        /// <param name="model">Reasoning model (e.g. MistralModels.MagistralMedium).</param>
        /// <param name="userMessage">The user's question or task.</param>
        /// <param name="useDefaultPrompt">If true, adds the default reasoning system prompt. Default is true.</param>
        public static ChatCompletionRequest CreateReasoningRequest(
            string model,
            string userMessage,
            bool useDefaultPrompt = true)
        {
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model is required.", nameof(model));
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("User message is required.", nameof(userMessage));

            var messages = new List<MessageRequest>();
            if (useDefaultPrompt)
            {
                messages.Add(MessageRequest.SystemWithChunks(DefaultReasoningSystemPrompt()));
            }
            messages.Add(MessageRequest.User(userMessage));

            return new ChatCompletionRequest
            {
                Model = model,
                Messages = messages,
                PromptMode = PromptModes.Reasoning
            };
        }
    }
}
