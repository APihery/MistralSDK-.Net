using MistralSDK.Abstractions;
using MistralSDK.ChatCompletion;
using MistralSDK.Embeddings;
using MistralSDK.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MistralSDK.Workflows
{
    /// <summary>
    /// Simple RAG workflow: add documents, embed them, then ask questions with retrieved context.
    /// </summary>
    public class SimpleRag
    {
        private readonly IMistralClient _client;
        private readonly string _embedModel;
        private readonly string _chatModel;
        private readonly List<RagChunk> _chunks = new();

        /// <summary>
        /// Gets the number of chunks currently stored.
        /// </summary>
        public int ChunkCount => _chunks.Count;

        /// <summary>
        /// Creates a new SimpleRag workflow.
        /// </summary>
        /// <param name="client">The Mistral client.</param>
        /// <param name="embedModel">Embedding model. Default mistral-embed.</param>
        /// <param name="chatModel">Chat model for answers. Default mistral-small-latest.</param>
        public SimpleRag(IMistralClient client, string? embedModel = null, string? chatModel = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _embedModel = embedModel ?? EmbeddingModels.MistralEmbed;
            _chatModel = chatModel ?? MistralModels.Small;
        }

        /// <summary>
        /// Adds a text chunk. Call AddDocumentsAsync to embed and index.
        /// </summary>
        public SimpleRag AddChunk(string text, string? sourceId = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return this;
            _chunks.Add(new RagChunk { Text = text.Trim(), SourceId = sourceId ?? string.Empty });
            return this;
        }

        /// <summary>
        /// Adds multiple chunks from a document, optionally split by paragraph or fixed size.
        /// </summary>
        /// <param name="document">The full document text.</param>
        /// <param name="sourceId">Optional source identifier.</param>
        /// <param name="splitByParagraphs">If true, splits by double newlines. Otherwise uses fixed chunk size.</param>
        /// <param name="chunkSize">When not splitting by paragraphs, max chars per chunk. Default 500.</param>
        public SimpleRag AddDocument(string document, string? sourceId = null, bool splitByParagraphs = true, int chunkSize = 500)
        {
            if (string.IsNullOrWhiteSpace(document))
                return this;

            if (splitByParagraphs)
            {
                var parts = document.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var trimmed = p.Trim();
                    if (trimmed.Length > 0)
                        AddChunk(trimmed, sourceId);
                }
            }
            else
            {
                for (var i = 0; i < document.Length; i += chunkSize)
                {
                    var len = Math.Min(chunkSize, document.Length - i);
                    var chunk = document.Substring(i, len).Trim();
                    if (chunk.Length > 0)
                        AddChunk(chunk, sourceId);
                }
            }
            return this;
        }

        /// <summary>
        /// Embeds all chunks. Call this after adding documents and before AskAsync.
        /// </summary>
        public async Task IndexAsync(CancellationToken cancellationToken = default)
        {
            if (_chunks.Count == 0)
                return;

            var texts = _chunks.Select(c => c.Text).ToArray();
            var response = await _client.EmbeddingsCreateAsync(texts, _embedModel, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccess)
                throw new InvalidOperationException($"Embedding failed: {response.Message}");

            var data = response.GetData<EmbeddingResponse>();
            if (data?.Data == null || data.Data.Count != _chunks.Count)
                throw new InvalidOperationException("Embedding response count mismatch.");

            for (var i = 0; i < _chunks.Count; i++)
                _chunks[i].Embedding = data.Data[i].Embedding;
        }

        /// <summary>
        /// Asks a question using retrieved context from the indexed documents.
        /// </summary>
        /// <param name="question">The user question.</param>
        /// <param name="topK">Number of chunks to retrieve. Default 5.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The assistant's answer.</returns>
        public async Task<string> AskAsync(string question, int topK = 5, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question is required.", nameof(question));
            if (_chunks.Count == 0)
                throw new InvalidOperationException("No chunks. Call AddDocument/AddChunk and IndexAsync first.");
            if (_chunks.Any(c => c.Embedding == null || c.Embedding.Count == 0))
                throw new InvalidOperationException("Chunks not indexed. Call IndexAsync first.");

            var queryResponse = await _client.EmbeddingsCreateAsync(question, _embedModel, cancellationToken).ConfigureAwait(false);
            if (!queryResponse.IsSuccess)
                throw new InvalidOperationException($"Query embedding failed: {queryResponse.Message}");

            var queryData = queryResponse.GetData<EmbeddingResponse>();
            var queryVec = queryData?.GetFirstVector() ?? new List<double>();
            if (queryVec.Count == 0)
                throw new InvalidOperationException("Empty query embedding.");

            var scored = _chunks
                .Where(c => c.Embedding != null && c.Embedding.Count > 0)
                .Select(c => new { Chunk = c, Score = CosineSimilarity(queryVec, c.Embedding!) })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();

            var context = string.Join("\n\n", scored.Select((x, i) => $"[{i + 1}] {x.Chunk.Text}"));

            var request = ChatContextBuilder.Create()
                .WithDocument(context, "Answer based only on the following retrieved passages. If the answer is not found, say so.")
                .WithUserQuestion(question)
                .WithModel(_chatModel)
                .Build();

            var chatResponse = await _client.ChatCompletionAsync(request, cancellationToken).ConfigureAwait(false);
            if (!chatResponse.IsSuccess)
                return string.Empty;

            var chatData = chatResponse.GetData<ChatCompletionResponse>();
            return chatData?.GetFirstChoiceContent() ?? string.Empty;
        }

        /// <summary>
        /// Clears all chunks and embeddings.
        /// </summary>
        public void Clear()
        {
            _chunks.Clear();
        }

        private static double CosineSimilarity(IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            if (a.Count != b.Count || a.Count == 0)
                return 0;

            double dot = 0, normA = 0, normB = 0;
            for (var i = 0; i < a.Count; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
            var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
            return denom > 0 ? dot / denom : 0;
        }

        private class RagChunk
        {
            public string Text { get; set; } = string.Empty;
            public string SourceId { get; set; } = string.Empty;
            public List<double>? Embedding { get; set; }
        }
    }
}
