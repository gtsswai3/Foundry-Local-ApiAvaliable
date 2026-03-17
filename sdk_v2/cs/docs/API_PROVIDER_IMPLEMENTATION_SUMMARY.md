# API Model Provider Support - Implementation Summary

## Overview

This implementation adds support for external API model providers to the Foundry Local SDK, allowing users to configure and use OpenAPI-compatible and Claude-compatible API providers alongside local models.

## Changes Made

### 1. Core Data Model Extensions

#### `FoundryModelInfo.cs`
- Added `ApiProviderConfig` record to store API provider configuration:
  - `Host`: API provider host URL
  - `ApiToken`: Authentication token
  - `ModelName`: Model name to use with the provider
- Extended `ModelInfo` record with optional `ApiProviderConfig` property

#### `Configuration.cs`
- Added `ApiProviders` dictionary property for managing multiple API provider configurations
- Added `ApiProviderSettings` nested class with:
  - `Host`: Provider URL
  - `ApiToken`: Authentication token
  - `ProviderType`: Provider type identifier (openai, claude, etc.)

### 2. Provider Implementation

Created new `Providers` namespace with the following components:

#### `IApiProviderChatClient.cs`
- Interface defining contract for API provider chat clients
- Methods: `CompleteChatAsync()`, `CompleteChatStreamingAsync()`

#### `OpenApiProviderChatClient.cs`
- Implementation for OpenAI-compatible APIs
- Supports both streaming and non-streaming requests
- Converts requests to OpenAI format and sends to `/v1/chat/completions` endpoint
- Handles streaming via Server-Sent Events (SSE)

#### `ClaudeProviderChatClient.cs`
- Implementation for Claude-compatible APIs (Anthropic)
- Converts OpenAI-format requests to Claude format
- Sends requests to `/v1/messages` endpoint
- Converts Claude responses back to OpenAI format for consistency
- Supports streaming via SSE

#### `ApiProviderChatClientFactory.cs`
- Factory class for creating appropriate provider clients based on `ProviderType`
- Supports provider types: `openai`, `openai-compatible`, `claude`, `claude-compatible`, `anthropic`

### 3. Client Integration

#### `OpenAI/ChatClient.cs`
- Extended `OpenAIChatClient` to support both local models and API providers
- Added constructor parameter for optional `IApiProviderChatClient`
- Modified `CompleteChatImplAsync()` to route to API provider when configured
- Modified `ChatStreamingImplAsync()` to route to API provider when configured
- Maintains backward compatibility with existing local model usage

#### `ModelVariant.cs`
- Updated `GetChatClientImplAsync()` to check for API provider configuration
- Creates chat client with API provider support when `ApiProviderConfig` is present
- Skips model loading check for API provider models

### 4. Utility Classes

#### `ApiProviderModelFactory.cs`
- Helper class with static factory methods for creating API provider models
- `CreateOpenAICompatibleModel()`: Creates OpenAI-compatible model configuration
- `CreateClaudeCompatibleModel()`: Creates Claude-compatible model configuration
- Simplifies the process of configuring API provider models

### 5. Documentation and Examples

#### `API_PROVIDER_EXAMPLE.md`
- Comprehensive documentation on using API providers
- Configuration examples (factory, Configuration object, ModelInfo)
- Usage examples with code snippets
- Security notes and limitations

#### `ApiProviderExample.cs`
- Complete working example demonstrating:
  - OpenAI-compatible API usage
  - Claude-compatible API usage
  - Custom OpenAI-compatible server usage (e.g., Ollama, LM Studio)

### 6. Serialization Support

#### `Detail/JsonSerializationContext.cs`
- Added `ApiProviderConfig` to JSON serialization context
- Enables proper serialization/deserialization of API provider configurations

## Key Features

1. **Unified Interface**: Same API for local and remote models
2. **Automatic Routing**: SDK routes requests based on `ProviderType`
3. **Multiple Providers**: Support for OpenAI and Claude APIs
4. **Streaming Support**: Both providers support streaming responses
5. **Flexible Configuration**: Multiple ways to configure providers
6. **Security**: Secure token storage via environment variables or configuration
7. **Backward Compatible**: Existing code continues to work without changes

## Supported Provider Types

- `openai` or `openai-compatible`: OpenAI, Azure OpenAI, local LLM servers
- `claude`, `claude-compatible`, or `anthropic`: Anthropic Claude API

## Usage Example

```csharp
// Create model configuration
var modelInfo = ApiProviderModelFactory.CreateOpenAICompatibleModel(
    id: "gpt-4",
    name: "GPT-4",
    host: "https://api.openai.com",
    apiToken: "your-api-key",
    modelName: "gpt-4"
);

// Get chat client
var logger = /* your logger */;
var apiProviderClient = ApiProviderChatClientFactory.Create(modelInfo, logger);
var chatClient = new OpenAIChatClient(modelInfo.Id, apiProviderClient);

// Use the client (same API as local models)
var messages = new List<ChatMessage> { /* your messages */ };
var response = await chatClient.CompleteChatAsync(messages);
```

## Architecture

```
User Code
    ↓
OpenAIChatClient
    ↓
    ├─→ Local Model Path (CoreInterop) ← for local models
    └─→ API Provider Path (IApiProviderChatClient)
            ↓
            ├─→ OpenApiProviderChatClient → OpenAI-compatible APIs
            └─→ ClaudeProviderChatClient → Claude-compatible APIs
```

## Testing Recommendations

1. Test with OpenAI API
2. Test with Anthropic Claude API
3. Test with local OpenAI-compatible servers (Ollama, LM Studio)
4. Test streaming and non-streaming modes
5. Test error handling (invalid tokens, network issues)
6. Verify backward compatibility with existing local model code

## Security Considerations

- Store API tokens in environment variables or secure configuration
- Never commit tokens to source control
- Use Azure Key Vault or similar for production deployments
- Validate and sanitize configuration inputs

## Limitations

- API provider models don't support local operations (`DownloadAsync`, `LoadAsync`, `UnloadAsync`)
- Audio transcription currently only supported for local models
- Tool/function calling support depends on underlying API provider capabilities

## Future Enhancements

Potential future improvements:
1. Automatic token refresh for providers that support it
2. Rate limiting and retry logic
3. Cost tracking and usage monitoring
4. Support for additional provider types (Hugging Face, etc.)
5. Audio transcription for cloud APIs
6. Model variant selection for API providers
