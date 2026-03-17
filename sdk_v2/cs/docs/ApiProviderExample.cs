// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

// Example: Using API Providers with Foundry Local SDK
// This example demonstrates how to use external API model providers (OpenAI-compatible or Claude-compatible)

using Microsoft.AI.Foundry.Local;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;

namespace ApiProviderExample;

class Program
{
    static async Task Main(string[] args)
    {
        // Example 1: Using OpenAI-compatible API
        await UseOpenAICompatibleAPI();

        // Example 2: Using Claude-compatible API
        await UseClaudeCompatibleAPI();

        // Example 3: Using custom OpenAI-compatible server
        await UseCustomOpenAIServer();
    }

    static async Task UseOpenAICompatibleAPI()
    {
        Console.WriteLine("=== OpenAI-compatible API Example ===\n");

        // Create model configuration for OpenAI
        var modelInfo = ApiProviderModelFactory.CreateOpenAICompatibleModel(
            id: "gpt-4",
            name: "GPT-4",
            host: "https://api.openai.com",
            apiToken: Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "your-openai-api-key",
            modelName: "gpt-4",
            supportsToolCalling: true,
            maxOutputTokens: 8192
        );

        // Get a chat client
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var apiProviderClient = Providers.ApiProviderChatClientFactory.Create(modelInfo, logger);

        if (apiProviderClient == null)
        {
            Console.WriteLine("Failed to create API provider client");
            return;
        }

        var chatClient = new OpenAIChatClient(modelInfo.Id, apiProviderClient);

        // Prepare messages
        var messages = new List<ChatMessage>
        {
            ChatMessage.FromSystem("You are a helpful assistant."),
            ChatMessage.FromUser("What is the capital of France? Answer in one sentence.")
        };

        try
        {
            // Non-streaming request
            Console.WriteLine("Sending non-streaming request...");
            var response = await chatClient.CompleteChatAsync(messages);
            Console.WriteLine($"Response: {response.Choices[0].Message.Content}\n");

            // Streaming request
            Console.WriteLine("Sending streaming request...");
            Console.Write("Response: ");
            await foreach (var chunk in chatClient.CompleteChatStreamingAsync(messages, CancellationToken.None))
            {
                if (chunk.Choices.Count > 0 && chunk.Choices[0].Delta?.Content != null)
                {
                    Console.Write(chunk.Choices[0].Delta.Content);
                }
            }
            Console.WriteLine("\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    static async Task UseClaudeCompatibleAPI()
    {
        Console.WriteLine("=== Claude-compatible API Example ===\n");

        // Create model configuration for Claude
        var modelInfo = ApiProviderModelFactory.CreateClaudeCompatibleModel(
            id: "claude-3-opus",
            name: "Claude 3 Opus",
            host: "https://api.anthropic.com",
            apiToken: Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? "your-anthropic-api-key",
            modelName: "claude-3-opus-20240229",
            maxOutputTokens: 4096
        );

        // Get a chat client
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var apiProviderClient = Providers.ApiProviderChatClientFactory.Create(modelInfo, logger);

        if (apiProviderClient == null)
        {
            Console.WriteLine("Failed to create API provider client");
            return;
        }

        var chatClient = new OpenAIChatClient(modelInfo.Id, apiProviderClient);

        // Prepare messages
        var messages = new List<ChatMessage>
        {
            ChatMessage.FromSystem("You are a helpful assistant."),
            ChatMessage.FromUser("Explain quantum computing in one sentence.")
        };

        try
        {
            // Non-streaming request
            Console.WriteLine("Sending non-streaming request...");
            var response = await chatClient.CompleteChatAsync(messages);
            Console.WriteLine($"Response: {response.Choices[0].Message.Content}\n");

            // Streaming request
            Console.WriteLine("Sending streaming request...");
            Console.Write("Response: ");
            await foreach (var chunk in chatClient.CompleteChatStreamingAsync(messages, CancellationToken.None))
            {
                if (chunk.Choices.Count > 0 && chunk.Choices[0].Delta?.Content != null)
                {
                    Console.Write(chunk.Choices[0].Delta.Content);
                }
            }
            Console.WriteLine("\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }
    }

    static async Task UseCustomOpenAIServer()
    {
        Console.WriteLine("=== Custom OpenAI-compatible Server Example ===\n");

        // Example: Using Ollama or LM Studio running locally
        var modelInfo = ApiProviderModelFactory.CreateOpenAICompatibleModel(
            id: "llama2-local",
            name: "Llama 2 (Local)",
            host: "http://localhost:11434",  // Ollama default port
            apiToken: "not-needed",  // Many local servers don't require auth
            modelName: "llama2",
            supportsToolCalling: false,
            maxOutputTokens: 2048
        );

        // Get a chat client
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var apiProviderClient = Providers.ApiProviderChatClientFactory.Create(modelInfo, logger);

        if (apiProviderClient == null)
        {
            Console.WriteLine("Failed to create API provider client");
            return;
        }

        var chatClient = new OpenAIChatClient(modelInfo.Id, apiProviderClient);

        // Prepare messages
        var messages = new List<ChatMessage>
        {
            ChatMessage.FromUser("Hello! Can you introduce yourself?")
        };

        try
        {
            Console.WriteLine("Connecting to local server...");
            var response = await chatClient.CompleteChatAsync(messages);
            Console.WriteLine($"Response: {response.Choices[0].Message.Content}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Make sure you have a local OpenAI-compatible server running (e.g., Ollama, LM Studio)\n");
        }
    }
}
