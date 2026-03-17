# Foundry Local API Format Support

Foundry Local now supports two API formats for model interactions:

1. **OpenAI-compatible API** (default)
2. **Claude (Anthropic) API**

This allows developers to choose the API format that best fits their needs or existing integrations.

## Table of Contents

- [Configuration](#configuration)
- [OpenAI API Format](#openai-api-format)
- [Claude API Format](#claude-api-format)
- [Key Differences](#key-differences)
- [Examples](#examples)

## Configuration

You can specify the API format when configuring the SDK:

### JavaScript/TypeScript

```typescript
import { FoundryLocalManager, ApiFormat } from 'foundry-local-sdk';

// OpenAI format (default)
const managerOpenAI = FoundryLocalManager.create({
    appName: 'my-app',
    webServiceApiFormat: ApiFormat.OpenAI
});

// Claude format
const managerClaude = FoundryLocalManager.create({
    appName: 'my-app',
    webServiceApiFormat: ApiFormat.Claude
});
```

### C#

```csharp
using Microsoft.AI.Foundry.Local;

// OpenAI format (default)
var configOpenAI = new Configuration
{
    AppName = "my-app",
    Web = new Configuration.WebService
    {
        Urls = "http://127.0.0.1:5000",
        ApiFormat = ApiFormat.OpenAI
    }
};

// Claude format
var configClaude = new Configuration
{
    AppName = "my-app",
    Web = new Configuration.WebService
    {
        Urls = "http://127.0.0.1:5000",
        ApiFormat = ApiFormat.Claude
    }
};

await FoundryLocalManager.CreateAsync(configClaude);
```

## OpenAI API Format

The OpenAI-compatible API follows the OpenAI Chat Completions format.

### Features

- Compatible with OpenAI SDK and libraries
- Supports system, user, assistant, and tool roles
- Tool calling support
- Streaming via SSE

### Example (JavaScript)

```typescript
const model = await manager.catalog.getModel('qwen2.5-0.5b');
await model.load();

const chatClient = model.createChatClient();
chatClient.settings.temperature = 0.7;
chatClient.settings.maxTokens = 512;

const response = await chatClient.completeChat([
    { role: 'system', content: 'You are a helpful assistant.' },
    { role: 'user', content: 'What is the golden ratio?' }
]);

console.log(response.choices[0].message.content);
```

### Request Format

```json
{
    "model": "qwen2.5-0.5b",
    "messages": [
        { "role": "system", "content": "You are a helpful assistant." },
        { "role": "user", "content": "Hello!" }
    ],
    "temperature": 0.7,
    "max_tokens": 512
}
```

### Response Format

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
                "content": "Hello! How can I help you today?"
            },
            "finish_reason": "stop"
        }
    ],
    "usage": {
        "prompt_tokens": 13,
        "completion_tokens": 9,
        "total_tokens": 22
    }
}
```

## Claude API Format

The Claude API follows the Anthropic Messages API format.

### Features

- Native Claude API compatibility
- Content blocks support (text, tool_use, tool_result)
- System message as a top-level parameter
- Streaming with structured events

### Example (JavaScript)

```typescript
const model = await manager.catalog.getModel('qwen2.5-0.5b');
await model.load();

const claudeClient = model.createClaudeClient();
claudeClient.settings.maxTokens = 512;
claudeClient.settings.temperature = 0.7;
claudeClient.settings.system = 'You are a helpful assistant.';

const response = await claudeClient.createMessage([
    { role: 'user', content: 'What is the golden ratio?' }
]);

for (const block of response.content) {
    if (block.type === 'text') {
        console.log(block.text);
    }
}
```

### Request Format

```json
{
    "model": "qwen2.5-0.5b",
    "messages": [
        { "role": "user", "content": "Hello!" }
    ],
    "system": "You are a helpful assistant.",
    "max_tokens": 512,
    "temperature": 0.7
}
```

### Response Format

```json
{
    "id": "msg_123",
    "type": "message",
    "role": "assistant",
    "content": [
        {
            "type": "text",
            "text": "Hello! How can I help you today?"
        }
    ],
    "model": "qwen2.5-0.5b",
    "stop_reason": "end_turn",
    "usage": {
        "input_tokens": 13,
        "output_tokens": 9
    }
}
```

## Key Differences

| Feature | OpenAI Format | Claude Format |
|---------|---------------|---------------|
| **System Message** | Part of messages array with role "system" | Top-level `system` parameter |
| **Message Roles** | system, user, assistant, tool | user, assistant (system is separate) |
| **Content Format** | String only | String or array of content blocks |
| **Token Usage** | `prompt_tokens`, `completion_tokens`, `total_tokens` | `input_tokens`, `output_tokens` |
| **Stop Reason** | `stop`, `length`, `tool_calls`, `content_filter` | `end_turn`, `max_tokens`, `tool_use`, `stop_sequence` |
| **Tool Calling** | `tool_calls` array in message | `tool_use` content blocks |
| **Streaming** | Delta format with `choices[0].delta` | Event-based with typed events |

## Examples

### Streaming with Both Formats

#### OpenAI Streaming

```typescript
await chatClient.completeStreamingChat(
    [{ role: 'user', content: 'Tell me a story' }],
    (chunk) => {
        if (chunk.choices[0]?.delta?.content) {
            process.stdout.write(chunk.choices[0].delta.content);
        }
    }
);
```

#### Claude Streaming

```typescript
await claudeClient.createMessageStreaming(
    [{ role: 'user', content: 'Tell me a story' }],
    (event) => {
        if (event.type === 'content_block_delta' && event.delta.type === 'text_delta') {
            process.stdout.write(event.delta.text);
        }
    }
);
```

### Content Blocks (Claude Format)

Claude API supports rich content blocks:

```typescript
const response = await claudeClient.createMessage([
    {
        role: 'user',
        content: [
            { type: 'text', text: 'Analyze this text: ' },
            { type: 'text', text: 'Hello, world!' }
        ]
    }
]);
```

### Switching Between Formats

You can use both formats with the same model:

```typescript
const model = await manager.catalog.getModel('qwen2.5-0.5b');
await model.load();

// OpenAI format
const openAIClient = model.createChatClient();
const openAIResponse = await openAIClient.completeChat([
    { role: 'user', content: 'Hello' }
]);

// Claude format
const claudeClient = model.createClaudeClient();
const claudeResponse = await claudeClient.createMessage([
    { role: 'user', content: 'Hello' }
]);
```

## Advanced Usage

### Tool Calling with Claude Format

```typescript
const tools = [
    {
        name: 'get_weather',
        description: 'Get the current weather for a location',
        input_schema: {
            type: 'object',
            properties: {
                location: { type: 'string', description: 'City name' }
            },
            required: ['location']
        }
    }
];

const response = await claudeClient.createMessage(
    [{ role: 'user', content: 'What\'s the weather in Paris?' }],
    { tools }
);

// Check for tool use
for (const block of response.content) {
    if (block.type === 'tool_use') {
        console.log(`Tool: ${block.name}`);
        console.log(`Input:`, block.input);
    }
}
```

## Migration Guide

### From OpenAI to Claude Format

If you're migrating from OpenAI format to Claude format:

1. **System messages**: Move system message from messages array to `system` parameter
2. **Response handling**: Change from `response.choices[0].message.content` to iterating over `response.content` blocks
3. **Token usage**: Update from `prompt_tokens`/`completion_tokens` to `input_tokens`/`output_tokens`
4. **Stop reasons**: Map OpenAI stop reasons to Claude equivalents

### From Claude to OpenAI Format

If you're migrating from Claude format to OpenAI format:

1. **System messages**: Add system message as first message with role "system"
2. **Content blocks**: Convert content block arrays to simple strings
3. **Response handling**: Access content via `response.choices[0].message.content`
4. **Token usage**: Update to use `prompt_tokens`/`completion_tokens`

## Best Practices

1. **Choose the right format**: Use OpenAI format for compatibility with existing tools, Claude format for native Anthropic integration
2. **Consistent usage**: Stick to one format throughout your application for consistency
3. **Error handling**: Both formats return structured errors, but the format differs
4. **Testing**: Test your application with both formats if supporting multiple integrations
5. **Performance**: Both formats have similar performance characteristics

## Support

For issues or questions:
- GitHub Issues: https://github.com/microsoft/Foundry-Local/issues
- Documentation: https://aka.ms/foundry-local-docs
- Discord: https://aka.ms/foundry-local-discord
