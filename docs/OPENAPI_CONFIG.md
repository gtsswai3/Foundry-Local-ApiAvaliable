# Foundry Local OpenAPI Configuration Guide

## Overview

Foundry Local now supports two API formats:
1. **OpenAI-compatible API** (default)
2. **Claude (Anthropic) API**

This guide explains how to configure the Foundry Local service to interface with OpenAPI-compatible APIs.

## Configuration Methods

### 1. JavaScript/TypeScript SDK Configuration

#### Default Configuration (OpenAPI Compatible)

```typescript
import { FoundryLocalManager, ApiFormat } from 'foundry-local-sdk';

// Option 1: Use default configuration (automatically uses OpenAI format)
const manager = FoundryLocalManager.create({
    appName: 'my-application'
});

// Option 2: Explicitly specify OpenAI format
const manager = FoundryLocalManager.create({
    appName: 'my-application',
    webServiceApiFormat: ApiFormat.OpenAI  // Explicitly specify OpenAI format
});
```

#### Complete Configuration Example

```typescript
import { FoundryLocalManager, ApiFormat } from 'foundry-local-sdk';

const manager = FoundryLocalManager.create({
    appName: 'my-application',
    webServiceUrls: 'http://127.0.0.1:5000',  // Specify service URL
    webServiceApiFormat: ApiFormat.OpenAI,     // OpenAPI-compatible format
    logLevel: 'info'                           // Log level
});

// Start web service
await manager.startWebService();
console.log('Web service started with OpenAPI-compatible format');
```

### 2. C# SDK Configuration

#### Default Configuration (OpenAPI Compatible)

```csharp
using Microsoft.AI.Foundry.Local;

// Option 1: Use default configuration (automatically uses OpenAI format)
var config = new Configuration
{
    AppName = "my-application",
    Web = new Configuration.WebService
    {
        Urls = "http://127.0.0.1:5000"
    }
};

// Option 2: Explicitly specify OpenAI format
var config = new Configuration
{
    AppName = "my-application",
    Web = new Configuration.WebService
    {
        Urls = "http://127.0.0.1:5000",
        ApiFormat = ApiFormat.OpenAI  // Explicitly specify OpenAI format
    }
};

await FoundryLocalManager.CreateAsync(config, logger);
var manager = FoundryLocalManager.Instance;

// Start web service
await manager.StartWebServiceAsync();
Console.WriteLine("Web service started with OpenAPI-compatible format");
```

### 3. Python SDK Configuration

```python
from foundry_local import FoundryLocalManager

# Python SDK uses OpenAI-compatible REST API by default
manager = FoundryLocalManager("qwen2.5-0.5b")

# Use OpenAI SDK
import openai
client = openai.OpenAI(
    base_url=manager.endpoint,
    api_key=manager.api_key
)

response = client.chat.completions.create(
    model=manager.get_model_info("qwen2.5-0.5b").id,
    messages=[{"role": "user", "content": "Hello"}]
)
```

## Using OpenAPI-Compatible Clients

### Method 1: Using Built-in ChatClient

```typescript
import { FoundryLocalManager } from 'foundry-local-sdk';

const manager = FoundryLocalManager.create({
    appName: 'my-app',
    webServiceApiFormat: ApiFormat.OpenAI  // OpenAPI-compatible format
});

const model = await manager.catalog.getModel('qwen2.5-0.5b');
await model.load();

// Create OpenAI-compatible ChatClient
const chatClient = model.createChatClient();

// Send request (OpenAI format)
const response = await chatClient.completeChat([
    { role: 'system', content: 'You are a helpful assistant.' },
    { role: 'user', content: 'What is the golden ratio?' }
]);

console.log(response.choices[0].message.content);
```

### Method 2: Using OpenAI SDK (Recommended for Existing Integrations)

```typescript
import OpenAI from 'openai';
import { FoundryLocalManager } from 'foundry-local-sdk';

// Start Foundry Local service
const manager = FoundryLocalManager.create({
    appName: 'my-app',
    webServiceUrls: 'http://127.0.0.1:5000',
    webServiceApiFormat: ApiFormat.OpenAI
});

await manager.startWebService();
const serviceUrl = manager.urls[0]; // Get actual bound URL

// Use standard OpenAI SDK to connect to local service
const client = new OpenAI({
    baseURL: `${serviceUrl}/v1`,
    apiKey: 'not-needed'  // Local service doesn't need real API key
});

const response = await client.chat.completions.create({
    model: 'qwen2.5-0.5b',
    messages: [
        { role: 'user', content: 'Hello!' }
    ]
});

console.log(response.choices[0].message.content);
```

### Method 3: Using C# OpenAI SDK

```csharp
using OpenAI;
using Microsoft.AI.Foundry.Local;

// Configure and start Foundry Local
var config = new Configuration
{
    AppName = "my-app",
    Web = new Configuration.WebService
    {
        Urls = "http://127.0.0.1:5000",
        ApiFormat = ApiFormat.OpenAI
    }
};

await FoundryLocalManager.CreateAsync(config, logger);
var manager = FoundryLocalManager.Instance;
await manager.StartWebServiceAsync();

// Use OpenAI SDK (e.g., Betalgo.OpenAI)
var openAIClient = new OpenAIClient(
    new ApiKeyCredential("not-needed"),
    new OpenAIClientOptions
    {
        Endpoint = new Uri($"{manager.Urls[0]}/v1")
    }
);

var chatClient = openAIClient.GetChatClient("qwen2.5-0.5b");
var response = await chatClient.CompleteChatAsync("Hello!");
```

## OpenAPI Endpoints

When configured for OpenAI-compatible format, Foundry Local provides these endpoints:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/v1/chat/completions` | POST | Chat completions (streaming and non-streaming) |
| `/v1/models` | GET | List downloaded models |
| `/v1/models/{model_id}` | GET | Get model details |
| `/v1/responses` | POST | Create response (Responses API) |
| `/v1/responses/{id}` | GET | Get response details |
| `/v1/responses/{id}` | DELETE | Delete response |
| `/v1/responses/{id}/cancel` | POST | Cancel in-progress response |

## OpenAPI Request Format

### Chat Completion Request Example

```json
{
    "model": "qwen2.5-0.5b",
    "messages": [
        {
            "role": "system",
            "content": "You are a helpful assistant."
        },
        {
            "role": "user",
            "content": "What is AI?"
        }
    ],
    "temperature": 0.7,
    "max_tokens": 512,
    "stream": false
}
```

### Response Format Example

```json
{
    "id": "chatcmpl-123",
    "object": "chat.completion",
    "created": 1677858242,
    "model": "qwen2.5-0.5b",
    "choices": [
        {
            "index": 0,
            "message": {
                "role": "assistant",
                "content": "AI (Artificial Intelligence) refers to..."
            },
            "finish_reason": "stop"
        }
    ],
    "usage": {
        "prompt_tokens": 20,
        "completion_tokens": 50,
        "total_tokens": 70
    }
}
```

## Streaming Responses

### Enable Streaming

```typescript
const chatClient = model.createChatClient();

await chatClient.completeStreamingChat(
    [{ role: 'user', content: 'Tell me a story' }],
    (chunk) => {
        // Handle streaming chunks
        if (chunk.choices[0]?.delta?.content) {
            process.stdout.write(chunk.choices[0].delta.content);
        }
    }
);
```

### Streaming Response Format

```json
data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1677858242,"model":"qwen2.5-0.5b","choices":[{"index":0,"delta":{"content":"Hello"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1677858242,"model":"qwen2.5-0.5b","choices":[{"index":0,"delta":{"content":" there"},"finish_reason":null}]}

data: [DONE]
```

## Tool Calling

OpenAPI format supports tool calling:

```typescript
const tools = [
    {
        type: 'function',
        function: {
            name: 'get_weather',
            description: 'Get the current weather',
            parameters: {
                type: 'object',
                properties: {
                    location: {
                        type: 'string',
                        description: 'City name'
                    }
                },
                required: ['location']
            }
        }
    }
];

const response = await chatClient.completeChat(
    [{ role: 'user', content: 'What\'s the weather in Paris?' }],
    tools
);

// Check for tool calls
if (response.choices[0].message.tool_calls) {
    console.log('Tool called:', response.choices[0].message.tool_calls);
}
```

## Environment Variable Configuration

You can also configure via environment variables:

```bash
# Set API format
export FOUNDRY_LOCAL_API_FORMAT=OpenAI

# Set service URL
export FOUNDRY_LOCAL_WEB_URL=http://127.0.0.1:5000

# Start application
node app.js
```

## Troubleshooting

### 1. Verify API Format Configuration

```typescript
// Print confirmation during configuration
const manager = FoundryLocalManager.create({
    appName: 'my-app',
    webServiceApiFormat: ApiFormat.OpenAI
});

console.log('API Format configured:', ApiFormat.OpenAI);
```

### 2. Test Endpoint Connectivity

```bash
# Test if service is running
curl http://127.0.0.1:5000/v1/models

# Test chat completion
curl -X POST http://127.0.0.1:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "qwen2.5-0.5b",
    "messages": [{"role": "user", "content": "Hello"}]
  }'
```

### 3. Check Logs

Enable verbose logging to diagnose issues:

```typescript
const manager = FoundryLocalManager.create({
    appName: 'my-app',
    webServiceApiFormat: ApiFormat.OpenAI,
    logLevel: 'debug'  // Enable verbose logging
});
```

## Best Practices

1. **Use Default Configuration**: OpenAI format is the default, no need to explicitly specify
2. **Port Configuration**: Use `127.0.0.1:0` to let system auto-assign available port
3. **Compatibility**: OpenAPI format is compatible with all OpenAI SDKs
4. **Error Handling**: Always handle network errors and model loading errors
5. **Resource Management**: Unload models promptly when done to free resources

## Example: Complete Workflow

```typescript
import { FoundryLocalManager, ApiFormat } from 'foundry-local-sdk';

async function main() {
    // 1. Create manager (OpenAPI-compatible format)
    const manager = FoundryLocalManager.create({
        appName: 'openapi-example',
        webServiceApiFormat: ApiFormat.OpenAI,
        logLevel: 'info'
    });

    // 2. Get and load model
    const model = await manager.catalog.getModel('qwen2.5-0.5b');

    if (!model.isCached) {
        console.log('Downloading model...');
        await model.download((progress) => {
            console.log(`Progress: ${progress.toFixed(2)}%`);
        });
    }

    await model.load();
    console.log('Model loaded');

    // 3. Create OpenAPI-compatible client
    const chatClient = model.createChatClient();
    chatClient.settings.temperature = 0.7;
    chatClient.settings.maxTokens = 512;

    // 4. Send request
    const response = await chatClient.completeChat([
        { role: 'system', content: 'You are a helpful assistant.' },
        { role: 'user', content: 'Explain quantum computing briefly.' }
    ]);

    console.log('Response:', response.choices[0].message.content);
    console.log('Tokens used:', response.usage.total_tokens);

    // 5. Cleanup
    await model.unload();
    console.log('Model unloaded');
}

main().catch(console.error);
```

## Reference Documentation

- [Complete API Formats Documentation](API_FORMATS.md)
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference)
- [Foundry Local Documentation](https://aka.ms/foundry-local-docs)

## Summary

Configuring Foundry Local to use OpenAPI-compatible format is simple:

1. **Default Works**: Foundry Local uses OpenAI-compatible format by default, no extra configuration needed
2. **Explicit Specification**: If needed, use `webServiceApiFormat: ApiFormat.OpenAI`
3. **Full Compatibility**: Can directly use OpenAI SDK to connect to local service
4. **Standard Endpoints**: Provides complete `/v1/*` OpenAPI endpoints
5. **Flexible Switching**: Can switch between OpenAI and Claude formats anytime
