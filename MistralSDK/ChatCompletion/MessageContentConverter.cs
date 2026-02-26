using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MistralSDK.ChatCompletion
{
    /// <summary>
    /// JSON converter for message content that can be either a string or an array of content chunks.
    /// </summary>
    public sealed class MessageContentConverter : JsonConverter<object?>
    {
        /// <inheritdoc />
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
                return null;
            if (reader.TokenType == JsonTokenType.String)
                return reader.GetString();
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var chunks = new List<ContentChunk>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;
                    try
                    {
                        var chunk = JsonSerializer.Deserialize<ContentChunk>(ref reader, options);
                        if (chunk != null)
                            chunks.Add(chunk);
                    }
                    catch (JsonException)
                    {
                        // Skip unknown or malformed chunk types (e.g. image_url, reference)
                    }
                }
                return chunks;
            }
            return null;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            if (value is string s)
            {
                writer.WriteStringValue(s);
                return;
            }
            if (value is List<ContentChunk> chunks)
            {
                writer.WriteStartArray();
                foreach (var chunk in chunks)
                    JsonSerializer.Serialize(writer, chunk, options);
                writer.WriteEndArray();
                return;
            }
            writer.WriteNullValue();
        }
    }
}
