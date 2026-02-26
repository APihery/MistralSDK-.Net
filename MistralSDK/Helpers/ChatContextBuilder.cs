using MistralSDK.ChatCompletion;
using System;
using System.Collections.Generic;

namespace MistralSDK.Helpers
{
    /// <summary>
    /// Fluent builder for constructing chat completion requests with document context.
    /// </summary>
    public class ChatContextBuilder
    {
        private string? _documentContent;
        private string? _documentInstruction;
        private string? _systemInstruction;
        private string? _userQuestion;
        private string _model = MistralModels.Small;
        private int? _maxTokens;
        private double? _temperature;

        private ChatContextBuilder() { }

        /// <summary>
        /// Creates a new chat context builder.
        /// </summary>
        public static ChatContextBuilder Create() => new();

        /// <summary>
        /// Sets the document content (e.g. from OCR) and optional instruction for how to use it.
        /// </summary>
        /// <param name="documentContent">The document text.</param>
        /// <param name="instruction">Optional instruction, e.g. "Answer only from this document."</param>
        public ChatContextBuilder WithDocument(string documentContent, string? instruction = null)
        {
            _documentContent = documentContent ?? string.Empty;
            _documentInstruction = instruction;
            return this;
        }

        /// <summary>
        /// Sets an additional system instruction (e.g. "Be concise.", "Respond in French.").
        /// </summary>
        public ChatContextBuilder WithInstruction(string instruction)
        {
            _systemInstruction = instruction;
            return this;
        }

        /// <summary>
        /// Sets the user question.
        /// </summary>
        public ChatContextBuilder WithUserQuestion(string question)
        {
            _userQuestion = question;
            return this;
        }

        /// <summary>
        /// Sets the model to use.
        /// </summary>
        public ChatContextBuilder WithModel(string model)
        {
            _model = model ?? MistralModels.Small;
            return this;
        }

        /// <summary>
        /// Sets the maximum tokens per response.
        /// </summary>
        public ChatContextBuilder WithMaxTokens(int maxTokens)
        {
            _maxTokens = maxTokens;
            return this;
        }

        /// <summary>
        /// Sets the temperature.
        /// </summary>
        public ChatContextBuilder WithTemperature(double temperature)
        {
            _temperature = temperature;
            return this;
        }

        /// <summary>
        /// Builds the chat completion request.
        /// </summary>
        public ChatCompletionRequest Build()
        {
            var systemParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(_documentContent))
            {
                var docInstruction = _documentInstruction ?? "Answer only based on the following document.";
                systemParts.Add($"{docInstruction}\n\n---\n{_documentContent}");
            }

            if (!string.IsNullOrWhiteSpace(_systemInstruction))
                systemParts.Add(_systemInstruction);

            var systemMessage = systemParts.Count > 0
                ? string.Join("\n\n", systemParts)
                : "You are a helpful assistant.";

            if (string.IsNullOrWhiteSpace(_userQuestion))
                throw new InvalidOperationException("User question is required. Call WithUserQuestion().");

            return new ChatCompletionRequest
            {
                Model = _model,
                Messages = new List<MessageRequest>
                {
                    MessageRequest.System(systemMessage),
                    MessageRequest.User(_userQuestion)
                },
                MaxTokens = _maxTokens,
                Temperature = _temperature
            };
        }
    }
}
