using MistralSDK;
using MistralSDK.ChatCompletion;

namespace MistralSDK.Examples
{
    /// <summary>
    /// Simple example demonstrating basic usage of the Mistral SDK.
    /// </summary>
    public class SimpleExample
    {
        /// <summary>
        /// Demonstrates a basic chat completion request.
        /// </summary>
        public static async Task RunBasicExample()
        {
            Console.WriteLine("=== Mistral SDK Basic Example ===\n");

            // Replace with your actual API key
            const string apiKey = "your-api-key-here";

            try
            {
                // Initialize the client
                using var client = new MistralClient(apiKey);

                // Create a simple chat completion request
                var request = new ChatCompletionRequest
                {
                    Model = MistralModels.Small,
                    Messages = new List<MessageRequest>
                    {
                        new MessageRequest
                        {
                            Role = MessageRoles.User,
                            Content = "Hello! Can you tell me a short joke?"
                        }
                    },
                    Temperature = 0.7,
                    MaxTokens = 100
                };

                Console.WriteLine("Sending request to Mistral AI...");
                Console.WriteLine($"Model: {request.Model}");
                Console.WriteLine($"User message: {request.Messages[0].Content}\n");

                // Send the request
                var response = await client.ChatCompletionAsync(request);

                // Handle the response
                if (response.IsSuccess)
                {
                    Console.WriteLine("✅ Success!");
                    Console.WriteLine($"Assistant: {response.Message}");
                    Console.WriteLine($"Model used: {response.Model}");
                    
                    if (response.Usage != null)
                    {
                        Console.WriteLine($"Tokens used: {response.Usage.TotalTokens}");
                        var cost = response.Usage.GetEstimatedCost(response.Model ?? "mistral-small");
                        Console.WriteLine($"Estimated cost: ${cost:F6}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Error occurred:");
                    Console.WriteLine($"Status code: {response.StatusCode}");
                    Console.WriteLine($"Error message: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates a conversation with multiple messages.
        /// </summary>
        public static async Task RunConversationExample()
        {
            Console.WriteLine("\n=== Mistral SDK Conversation Example ===\n");

            // Replace with your actual API key
            const string apiKey = "your-api-key-here";

            try
            {
                using var client = new MistralClient(apiKey);

                // Create a conversation with system context
                var conversation = new List<MessageRequest>
                {
                    new MessageRequest
                    {
                        Role = MessageRoles.System,
                        Content = "You are a helpful programming assistant. Provide clear, concise answers."
                    },
                    new MessageRequest
                    {
                        Role = MessageRoles.User,
                        Content = "What is the difference between 'var' and explicit types in C#?"
                    }
                };

                var request = new ChatCompletionRequest
                {
                    Model = MistralModels.Medium,
                    Messages = conversation,
                    Temperature = 0.3, // Lower temperature for more focused technical answers
                    MaxTokens = 300
                };

                Console.WriteLine("Sending programming question to Mistral AI...\n");

                var response = await client.ChatCompletionAsync(request);

                if (response.IsSuccess)
                {
                    Console.WriteLine("✅ Response received:");
                    Console.WriteLine($"Assistant: {response.Message}\n");

                    // Add the assistant's response to the conversation
                    conversation.Add(new MessageRequest
                    {
                        Role = MessageRoles.Assistant,
                        Content = response.Message
                    });

                    // Add a follow-up question
                    conversation.Add(new MessageRequest
                    {
                        Role = MessageRoles.User,
                        Content = "When should I use 'var' and when should I use explicit types?"
                    });

                    // Create a new request with the updated conversation
                    var followUpRequest = new ChatCompletionRequest
                    {
                        Model = MistralModels.Medium,
                        Messages = conversation,
                        Temperature = 0.3,
                        MaxTokens = 200
                    };

                    Console.WriteLine("Sending follow-up question...\n");

                    var followUpResponse = await client.ChatCompletionAsync(followUpRequest);

                    if (followUpResponse.IsSuccess)
                    {
                        Console.WriteLine("✅ Follow-up response:");
                        Console.WriteLine($"Assistant: {followUpResponse.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Follow-up error: {followUpResponse.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Error: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Demonstrates error handling and validation.
        /// </summary>
        public static async Task RunErrorHandlingExample()
        {
            Console.WriteLine("\n=== Mistral SDK Error Handling Example ===\n");

            try
            {
                // This will throw an exception due to invalid API key
                using var client = new MistralClient("invalid-api-key");

                var request = new ChatCompletionRequest
                {
                    Model = MistralModels.Small,
                    Messages = new List<MessageRequest>
                    {
                        new MessageRequest
                        {
                            Role = MessageRoles.User,
                            Content = "Hello"
                        }
                    }
                };

                Console.WriteLine("Testing with invalid API key...");
                var response = await client.ChatCompletionAsync(request);

                if (!response.IsSuccess)
                {
                    Console.WriteLine($"❌ Expected error occurred:");
                    Console.WriteLine($"Status code: {response.StatusCode}");
                    Console.WriteLine($"Error message: {response.Message}");
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"❌ Validation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
            }

            // Test request validation
            Console.WriteLine("\nTesting request validation...");
            var invalidRequest = new ChatCompletionRequest
            {
                Model = "", // Invalid: empty model
                Messages = new List<MessageRequest>(), // Invalid: empty messages
                Temperature = 3.0 // Invalid: temperature > 2.0
            };

            if (!invalidRequest.IsValid())
            {
                Console.WriteLine("❌ Request validation correctly identified invalid parameters");
            }
        }

        /// <summary>
        /// Main method to run all examples.
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Mistral AI SDK for .NET - Examples\n");

            // Run basic example
            await RunBasicExample();

            // Run conversation example
            await RunConversationExample();

            // Run error handling example
            await RunErrorHandlingExample();

            Console.WriteLine("\n=== Examples completed ===");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
} 