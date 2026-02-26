using MistralSDK.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MistralSDK.Helpers
{
    /// <summary>
    /// Helpers for managing conversation context and history.
    /// </summary>
    public static class ConversationHelper
    {
        /// <summary>
        /// Trims the message list to keep only the system message (if any) and the last N user/assistant exchanges.
        /// Useful when the conversation exceeds the context window.
        /// </summary>
        /// <param name="messages">The full message list.</param>
        /// <param name="maxExchanges">Maximum number of user/assistant turns to keep. Default 10.</param>
        /// <returns>A new list with trimmed messages.</returns>
        public static List<MessageRequest> TrimToLastMessages(
            IReadOnlyList<MessageRequest> messages,
            int maxExchanges = 10)
        {
            if (messages == null || messages.Count == 0)
                return new List<MessageRequest>();

            var result = new List<MessageRequest>();
            var systemMessages = messages.Where(m => m.Role.Equals(MessageRoles.System, StringComparison.OrdinalIgnoreCase)).ToList();
            var nonSystem = messages.Where(m => !m.Role.Equals(MessageRoles.System, StringComparison.OrdinalIgnoreCase)).ToList();

            result.AddRange(systemMessages);

            var exchanges = nonSystem.Count / 2;
            var toSkip = Math.Max(0, exchanges - maxExchanges) * 2;
            var toTake = nonSystem.Count - toSkip;

            if (toTake > 0)
                result.AddRange(nonSystem.Skip(toSkip).Take(toTake));

            return result;
        }

        /// <summary>
        /// Keeps the last N messages (including system), removing older ones from the start.
        /// </summary>
        /// <param name="messages">The full message list.</param>
        /// <param name="maxMessages">Maximum total messages to keep. Default 20.</param>
        /// <returns>A new list with trimmed messages.</returns>
        public static List<MessageRequest> TrimToLastN(
            IReadOnlyList<MessageRequest> messages,
            int maxMessages = 20)
        {
            if (messages == null || messages.Count == 0)
                return new List<MessageRequest>();

            if (messages.Count <= maxMessages)
                return new List<MessageRequest>(messages);

            return messages.Skip(messages.Count - maxMessages).ToList();
        }

        /// <summary>
        /// Extracts the last assistant message from the conversation.
        /// </summary>
        public static string? GetLastAssistantMessage(IReadOnlyList<MessageRequest> messages)
        {
            if (messages == null || messages.Count == 0)
                return null;

            for (var i = messages.Count - 1; i >= 0; i--)
            {
                var m = messages[i];
                if (m.Role.Equals(MessageRoles.Assistant, StringComparison.OrdinalIgnoreCase))
                    return m.Content;
            }

            return null;
        }

        /// <summary>
        /// Extracts the last user message from the conversation.
        /// </summary>
        public static string? GetLastUserMessage(IReadOnlyList<MessageRequest> messages)
        {
            if (messages == null || messages.Count == 0)
                return null;

            for (var i = messages.Count - 1; i >= 0; i--)
            {
                var m = messages[i];
                if (m.Role.Equals(MessageRoles.User, StringComparison.OrdinalIgnoreCase))
                    return m.Content;
            }

            return null;
        }
    }
}
