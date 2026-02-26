using MistralSDK.Abstractions;
using MistralSDK.Conversation;
using MistralSDK.Ocr;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MistralSDK.Workflows
{
    /// <summary>
    /// Document Q&amp;A workflow: loads a document via OCR, then answers questions with conversation history.
    /// </summary>
    public class DocumentQa
    {
        private readonly IMistralClient _client;
        private readonly string _chatModel;
        private readonly string _ocrModel;
        private ChatSession? _session;
        private string _documentText = string.Empty;

        /// <summary>
        /// Gets the extracted document text (empty until LoadDocumentAsync is called).
        /// </summary>
        public string DocumentText => _documentText;

        /// <summary>
        /// Gets whether a document has been loaded.
        /// </summary>
        public bool IsLoaded => !string.IsNullOrWhiteSpace(_documentText);

        /// <summary>
        /// Creates a new DocumentQa workflow.
        /// </summary>
        /// <param name="client">The Mistral client.</param>
        /// <param name="chatModel">Model for Q&amp;A. Default mistral-small-latest.</param>
        /// <param name="ocrModel">Model for OCR. Default mistral-ocr-latest.</param>
        public DocumentQa(IMistralClient client, string? chatModel = null, string? ocrModel = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _chatModel = chatModel ?? MistralSDK.ChatCompletion.MistralModels.Small;
            _ocrModel = ocrModel ?? OcrModels.MistralOcrLatest;
        }

        /// <summary>
        /// Loads a document from a stream (PDF or image) and extracts text via OCR.
        /// </summary>
        /// <param name="fileStream">The document stream.</param>
        /// <param name="fileName">The file name (e.g. document.pdf).</param>
        /// <param name="deleteFileAfterOcr">If true, deletes the uploaded file after OCR. Default true.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task LoadDocumentAsync(Stream fileStream, string fileName, bool deleteFileAfterOcr = true, CancellationToken cancellationToken = default)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required.", nameof(fileName));

            _documentText = await _client.OcrExtractTextAsync(fileStream, fileName, deleteFileAfterOcr, cancellationToken).ConfigureAwait(false);
            _session = null;
        }

        /// <summary>
        /// Loads document text directly (e.g. from a previous OCR or plain text file).
        /// </summary>
        public void LoadDocumentText(string text)
        {
            _documentText = text ?? string.Empty;
            _session = null;
        }

        /// <summary>
        /// Asks a question about the document. Keeps conversation history for follow-up questions.
        /// </summary>
        /// <param name="question">The user question.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The assistant's answer.</returns>
        public async Task<string> AskAsync(string question, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question is required.", nameof(question));
            if (!IsLoaded)
                throw new InvalidOperationException("No document loaded. Call LoadDocumentAsync or LoadDocumentText first.");

            _session ??= CreateSession();
            _session.AddUser(question);
            return await _session.CompleteAsync(addToHistory: true, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Clears the conversation history. The document remains loaded.
        /// </summary>
        public void ClearHistory()
        {
            _session?.Clear(keepSystemPrompt: true);
        }

        /// <summary>
        /// Resets the workflow: clears document and history.
        /// </summary>
        public void Reset()
        {
            _documentText = string.Empty;
            _session = null;
        }

        private ChatSession CreateSession()
        {
            var session = new ChatSession(_client, _chatModel);
            session.SystemPrompt = $"You are a helpful assistant. Answer questions based ONLY on the following document. If the answer is not in the document, say so.\n\n---\n{_documentText}";
            return session;
        }
    }
}
