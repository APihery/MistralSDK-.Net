using MistralSDK.Abstractions;
using MistralSDK.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MistralSDK.Conversation
{
    /// <summary>
    /// Manages conversation history and provides a simple API for multi-turn chat.
    /// </summary>
    public class ChatSession
    {
        private readonly IMistralClient _client;
        private readonly List<MessageRequest> _messages = new();
        private readonly string _model;
        private readonly int? _maxTokens;
        private readonly double? _temperature;

        /// <summary>
        /// Gets or sets the optional system prompt. When set, it is prepended to the conversation.
        /// </summary>
        public string? SystemPrompt { get; set; }

        /// <summary>
        /// Gets the current message history (read-only).
        /// </summary>
        public IReadOnlyList<MessageRequest> Messages => _messages;

        /// <summary>
        /// Creates a new chat session.
        /// </summary>
        /// <param name="client">The Mistral client.</param>
        /// <param name="model">Model to use (default: mistral-small-latest).</param>
        /// <param name="maxTokens">Maximum tokens per response.</param>
        /// <param name="temperature">Sampling temperature.</param>
        public ChatSession(
            IMistralClient client,
            string model = MistralModels.Small,
            int? maxTokens = null,
            double? temperature = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _model = model ?? MistralModels.Small;
            _maxTokens = maxTokens;
            _temperature = temperature;
        }

        /// <summary>
        /// Adds a user message to the conversation.
        /// </summary>
        public ChatSession AddUser(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("User message content is required.", nameof(content));
            _messages.Add(MessageRequest.User(content));
            return this;
        }

        /// <summary>
        /// Adds an assistant message to the conversation (e.g. after receiving a response).
        /// </summary>
        public ChatSession AddAssistant(string content)
        {
            if (content == null)
                content = string.Empty;
            _messages.Add(MessageRequest.Assistant(content));
            return this;
        }

        /// <summary>
        /// Adds a system message. Typically used when SystemPrompt is not set.
        /// </summary>
        public ChatSession AddSystem(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("System message content is required.", nameof(content));
            _messages.Add(MessageRequest.System(content));
            return this;
        }

        /// <summary>
        /// Sends the current conversation to the API and returns the assistant's reply.
        /// Automatically adds the assistant response to history when successful.
        /// </summary>
        /// <param name="addToHistory">Whether to add the assistant reply to history. Default true.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The assistant's reply text, or empty string on failure.</returns>
        public async Task<string> CompleteAsync(bool addToHistory = true, CancellationToken cancellationToken = default)
        {
            var request = BuildRequest();
            var response = await _client.ChatCompletionAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccess)
                return string.Empty;

            var data = response.GetData<ChatCompletionResponse>();
            var content = data?.GetFirstChoiceContent() ?? string.Empty;

            if (addToHistory && !string.IsNullOrEmpty(content))
                AddAssistant(content);

            return content;
        }

        /// <summary>
        /// Sends the current conversation and streams the response.
        /// </summary>
        public async IAsyncEnumerable<string> CompleteStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var request = BuildRequest();
            var fullContent = new System.Text.StringBuilder();

            await foreach (var chunk in _client.ChatCompletionStreamAsync(request, cancellationToken).ConfigureAwait(false))
            {
                var text = chunk.GetContent();
                if (!string.IsNullOrEmpty(text))
                {
                    fullContent.Append(text);
                    yield return text;
                }
            }

            if (fullContent.Length > 0)
                AddAssistant(fullContent.ToString());
        }

        /// <summary>
        /// Clears the conversation history (optionally keeps the system prompt).
        /// </summary>
        /// <param name="keepSystemPrompt">If true and SystemPrompt is set, keeps the system message.</param>
        public void Clear(bool keepSystemPrompt = false)
        {
            _messages.Clear();
            if (keepSystemPrompt && !string.IsNullOrWhiteSpace(SystemPrompt))
                AddSystem(SystemPrompt);
        }

        /// <summary>
        /// Builds the chat completion request from current messages.
        /// </summary>
        public ChatCompletionRequest BuildRequest()
        {
            var messages = new List<MessageRequest>();

            if (!string.IsNullOrWhiteSpace(SystemPrompt))
                messages.Add(MessageRequest.System(SystemPrompt));

            messages.AddRange(_messages);

            if (messages.Count == 0)
                throw new InvalidOperationException("No messages in the conversation. Add at least one user message first.");

            return new ChatCompletionRequest
            {
                Model = _model,
                Messages = messages,
                MaxTokens = _maxTokens,
                Temperature = _temperature
            };
        }
    }
}
