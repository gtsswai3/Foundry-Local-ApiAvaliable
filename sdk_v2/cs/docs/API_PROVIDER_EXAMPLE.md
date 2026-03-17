# API Provider Example

This example demonstrates how to use external API model providers (OpenAI-compatible or Claude-compatible) with Foundry Local SDK.

## Overview

The Foundry Local SDK now supports connecting to external API providers in addition to local models. This allows you to:
- Use OpenAI-compatible APIs (OpenAI, Azure OpenAI, local LLM servers, etc.)
- Use Claude-compatible APIs (Anthropic Claude)
- Maintain the same programming interface regardless of whether you're using local or remote models

## Configuration

### Option 1: Using ApiProviderModelFactory

The easiest way to create API provider models is using the `ApiProviderModelFactory`:

```csharp
using Microsoft.AI.Foundry.Local;

// Create an OpenAI-compatible model
var openAIModel = ApiProviderModelFactory.CreateOpenAICompatibleModel(
    id: "gpt-4",
    name: "GPT-4",
    host: "https://api.openai.com",
    apiToken: "your-openai-api-key",
    modelName: "gpt-4",
    supportsToolCalling: true,
    maxOutputTokens: 8192
);

// Create a Claude-compatible model
var claudeModel = ApiProviderModelFactory.CreateClaudeCompatibleModel(
    id: "claude-3-opus",
    name: "Claude 3 Opus",
    host: "https://api.anthropic.com",
    apiToken: "your-anthropic-api-key",
    modelName: "claude-3-opus-20240229",
    maxOutputTokens: 4096
);
```

### Option 2: Using Configuration

You can also configure API providers in the Configuration object:

```csharp
var config = new Configuration
{
    AppName = "MyApp",
    ApiProviders = new Dictionary<string, Configuration.ApiProviderSettings>
    {
        ["openai"] = new Configuration.ApiProviderSettings
        {
            Host = "https://api.openai.com",
            ApiToken = "your-openai-api-key",
            ProviderType = "openai"
        },
        ["anthropic"] = new Configuration.ApiProviderSettings
        {
            Host = "https://api.anthropic.com",
            ApiToken = "your-anthropic-api-key",
            ProviderType = "claude"
        }
    }
};
```

### Option 3: Using ModelInfo with ApiProviderConfig

For full control, create ModelInfo with ApiProviderConfig:

```csharp
var modelInfo = new ModelInfo
{
    Id = "my-custom-model",
    Name = "My Custom Model",
    Version = 1,
    Alias = "my-custom-model",
    ProviderType = "openai-compatible",
    Uri = "https://my-api.example.com",
    ModelType = "chat",
    Cached = false,
    ApiProviderConfig = new ApiProviderConfig
    {
        Host = "https://my-api.example.com",
        ApiToken = "my-api-token",
        ModelName = "custom-model-v1"
    }
};
```

## Usage Example

```csharp
using Microsoft.AI.Foundry.Local;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;

// Create model configuration
var modelInfo = ApiProviderModelFactory.CreateOpenAICompatibleModel(
    id: "gpt-4",
    name: "GPT-4",
    host: "https://api.openai.com",
    apiToken: Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "your-key",
    modelName: "gpt-4",
    supportsToolCalling: true
);

// Create a model variant (wrapper around ModelInfo)
// Note: For API providers, you'll need to create the variant manually
// or extend the catalog to support API provider models

// Get a chat client
// For demonstration, we'll show how the client works with API providers
var chatClient = new OpenAIChatClient(modelInfo.Id,
    Providers.ApiProviderChatClientFactory.Create(modelInfo, logger));

// Use the chat client (same API as local models)
var messages = new List<ChatMessage>
{
    ChatMessage.FromSystem("You are a helpful assistant."),
    ChatMessage.FromUser("What is the capital of France?")
};

var response = await chatClient.CompleteChatAsync(messages);
Console.WriteLine(response.Choices[0].Message.Content);

// Streaming is also supported
await foreach (var chunk in chatClient.CompleteChatStreamingAsync(messages, CancellationToken.None))
{
    if (chunk.Choices.Count > 0 && chunk.Choices[0].Delta?.Content != null)
    {
        Console.Write(chunk.Choices[0].Delta.Content);
    }
}
```

## Supported Provider Types

The following provider types are supported:

- `"openai"` or `"openai-compatible"` - OpenAI-compatible APIs
  - OpenAI
  - Azure OpenAI
  - Local LLM servers (Ollama, LM Studio, etc.)
  - Any service with OpenAI-compatible `/v1/chat/completions` endpoint

- `"claude"`, `"claude-compatible"`, or `"anthropic"` - Claude-compatible APIs
  - Anthropic Claude
  - Any service with Claude-compatible `/v1/messages` endpoint

## Key Features

1. **Unified Interface**: Use the same `OpenAIChatClient` API for both local and remote models
2. **Automatic Routing**: The SDK automatically routes requests to the appropriate provider based on `ProviderType`
3. **No Loading Required**: API provider models don't need to be loaded or cached locally
4. **Streaming Support**: Both providers support streaming responses
5. **Compatible API**: The external API interface remains unchanged from the user's perspective

## Security Notes

- Store API tokens securely using environment variables or secure configuration storage
- Never commit API tokens to source control
- Consider using Azure Key Vault or similar services for production deployments

## Limitations

- API provider models don't support local operations like `DownloadAsync()`, `LoadAsync()`, or `UnloadAsync()`
- Tool/function calling support depends on the underlying API provider
- Audio transcription is currently only supported for local models
