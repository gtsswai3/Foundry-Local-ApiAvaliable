# Foundry Local OpenAPI 兼容配置指南

## 概述

Foundry Local 现在支持两种 API 格式：
1. **OpenAI 兼容 API**（默认）
2. **Claude (Anthropic) API**

本指南说明如何配置 Foundry Local 服务程序以对接 OpenAPI 兼容的 API。

## 配置方法

### 1. JavaScript/TypeScript SDK 配置

#### 默认配置（OpenAPI 兼容）

```typescript
import { FoundryLocalManager, ApiFormat } from 'foundry-local-sdk';

// 方式 1: 使用默认配置（自动使用 OpenAI 格式）
const manager = FoundryLocalManager.create({
    appName: 'my-application'
});

// 方式 2: 明确指定 OpenAI 格式
const manager = FoundryLocalManager.create({
    appName: 'my-application',
    webServiceApiFormat: ApiFormat.OpenAI  // 明确指定 OpenAI 格式
});
```

#### 完整配置示例

```typescript
import { FoundryLocalManager, ApiFormat } from 'foundry-local-sdk';

const manager = FoundryLocalManager.create({
    appName: 'my-application',
    webServiceUrls: 'http://127.0.0.1:5000',  // 指定服务 URL
    webServiceApiFormat: ApiFormat.OpenAI,     // OpenAPI 兼容格式
    logLevel: 'info'                           // 日志级别
});

// 启动 Web 服务
await manager.startWebService();
console.log('Web service started with OpenAPI-compatible format');
```

### 2. C# SDK 配置

#### 默认配置（OpenAPI 兼容）

```csharp
using Microsoft.AI.Foundry.Local;

// 方式 1: 使用默认配置（自动使用 OpenAI 格式）
var config = new Configuration
{
    AppName = "my-application",
    Web = new Configuration.WebService
    {
        Urls = "http://127.0.0.1:5000"
    }
};

// 方式 2: 明确指定 OpenAI 格式
var config = new Configuration
{
    AppName = "my-application",
    Web = new Configuration.WebService
    {
        Urls = "http://127.0.0.1:5000",
        ApiFormat = ApiFormat.OpenAI  // 明确指定 OpenAI 格式
    }
};

await FoundryLocalManager.CreateAsync(config, logger);
var manager = FoundryLocalManager.Instance;

// 启动 Web 服务
await manager.StartWebServiceAsync();
Console.WriteLine("Web service started with OpenAPI-compatible format");
```

### 3. Python SDK 配置

```python
from foundry_local import FoundryLocalManager

# Python SDK 默认使用 OpenAI 兼容的 REST API
manager = FoundryLocalManager("qwen2.5-0.5b")

# 使用 OpenAI SDK
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

## 使用 OpenAPI 兼容客户端

### 方式 1: 使用内置 ChatClient

```typescript
import { FoundryLocalManager } from 'foundry-local-sdk';

const manager = FoundryLocalManager.create({
    appName: 'my-app',
    webServiceApiFormat: ApiFormat.OpenAI  // OpenAPI 兼容格式
});

const model = await manager.catalog.getModel('qwen2.5-0.5b');
await model.load();

// 创建 OpenAI 兼容的 ChatClient
const chatClient = model.createChatClient();

// 发送请求（OpenAI 格式）
const response = await chatClient.completeChat([
    { role: 'system', content: 'You are a helpful assistant.' },
    { role: 'user', content: 'What is the golden ratio?' }
]);

console.log(response.choices[0].message.content);
```

### 方式 2: 使用 OpenAI SDK（推荐用于现有集成）

```typescript
import OpenAI from 'openai';
import { FoundryLocalManager } from 'foundry-local-sdk';

// 启动 Foundry Local 服务
const manager = FoundryLocalManager.create({
    appName: 'my-app',
    webServiceUrls: 'http://127.0.0.1:5000',
    webServiceApiFormat: ApiFormat.OpenAI
});

await manager.startWebService();
const serviceUrl = manager.urls[0]; // 获取实际绑定的 URL

// 使用标准 OpenAI SDK 连接到本地服务
const client = new OpenAI({
    baseURL: `${serviceUrl}/v1`,
    apiKey: 'not-needed'  // 本地服务不需要实际的 API key
});

const response = await client.chat.completions.create({
    model: 'qwen2.5-0.5b',
    messages: [
        { role: 'user', content: 'Hello!' }
    ]
});

console.log(response.choices[0].message.content);
```

### 方式 3: 使用 C# OpenAI SDK

```csharp
using OpenAI;
using Microsoft.AI.Foundry.Local;

// 配置并启动 Foundry Local
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

// 使用 OpenAI SDK（例如 Betalgo.OpenAI）
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

## OpenAPI 端点说明

当配置为 OpenAI 兼容格式时，Foundry Local 提供以下端点：

| 端点 | 方法 | 说明 |
|------|------|------|
| `/v1/chat/completions` | POST | 聊天补全（支持流式和非流式） |
| `/v1/models` | GET | 列出已下载的模型 |
| `/v1/models/{model_id}` | GET | 获取模型详情 |
| `/v1/responses` | POST | 创建响应（Responses API） |
| `/v1/responses/{id}` | GET | 获取响应详情 |
| `/v1/responses/{id}` | DELETE | 删除响应 |
| `/v1/responses/{id}/cancel` | POST | 取消进行中的响应 |

## OpenAPI 请求格式

### 聊天补全请求示例

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

### 响应格式示例

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

## 流式响应

### 启用流式响应

```typescript
const chatClient = model.createChatClient();

await chatClient.completeStreamingChat(
    [{ role: 'user', content: 'Tell me a story' }],
    (chunk) => {
        // 处理流式数据块
        if (chunk.choices[0]?.delta?.content) {
            process.stdout.write(chunk.choices[0].delta.content);
        }
    }
);
```

### 流式响应格式

```json
data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1677858242,"model":"qwen2.5-0.5b","choices":[{"index":0,"delta":{"content":"Hello"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1677858242,"model":"qwen2.5-0.5b","choices":[{"index":0,"delta":{"content":" there"},"finish_reason":null}]}

data: [DONE]
```

## 工具调用（Tool Calling）

OpenAPI 格式支持工具调用：

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

// 检查工具调用
if (response.choices[0].message.tool_calls) {
    console.log('Tool called:', response.choices[0].message.tool_calls);
}
```

## 环境变量配置

也可以通过环境变量配置：

```bash
# 设置 API 格式
export FOUNDRY_LOCAL_API_FORMAT=OpenAI

# 设置服务 URL
export FOUNDRY_LOCAL_WEB_URL=http://127.0.0.1:5000

# 启动应用
node app.js
```

## 故障排查

### 1. 确认 API 格式配置

```typescript
// 在配置时打印确认
const manager = FoundryLocalManager.create({
    appName: 'my-app',
    webServiceApiFormat: ApiFormat.OpenAI
});

console.log('API Format configured:', ApiFormat.OpenAI);
```

### 2. 测试端点连接

```bash
# 测试服务是否正常运行
curl http://127.0.0.1:5000/v1/models

# 测试聊天补全
curl -X POST http://127.0.0.1:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "qwen2.5-0.5b",
    "messages": [{"role": "user", "content": "Hello"}]
  }'
```

### 3. 检查日志

启用详细日志以诊断问题：

```typescript
const manager = FoundryLocalManager.create({
    appName: 'my-app',
    webServiceApiFormat: ApiFormat.OpenAI,
    logLevel: 'debug'  // 启用详细日志
});
```

## 最佳实践

1. **使用默认配置**: OpenAI 格式是默认配置，无需显式指定
2. **端口配置**: 使用 `127.0.0.1:0` 让系统自动分配可用端口
3. **兼容性**: OpenAPI 格式与所有 OpenAI SDK 兼容
4. **错误处理**: 始终处理网络错误和模型加载错误
5. **资源管理**: 使用完毕后及时卸载模型释放资源

## 示例：完整工作流程

```typescript
import { FoundryLocalManager, ApiFormat } from 'foundry-local-sdk';

async function main() {
    // 1. 创建管理器（OpenAPI 兼容格式）
    const manager = FoundryLocalManager.create({
        appName: 'openapi-example',
        webServiceApiFormat: ApiFormat.OpenAI,
        logLevel: 'info'
    });

    // 2. 获取并加载模型
    const model = await manager.catalog.getModel('qwen2.5-0.5b');

    if (!model.isCached) {
        console.log('Downloading model...');
        await model.download((progress) => {
            console.log(`Progress: ${progress.toFixed(2)}%`);
        });
    }

    await model.load();
    console.log('Model loaded');

    // 3. 创建 OpenAPI 兼容客户端
    const chatClient = model.createChatClient();
    chatClient.settings.temperature = 0.7;
    chatClient.settings.maxTokens = 512;

    // 4. 发送请求
    const response = await chatClient.completeChat([
        { role: 'system', content: 'You are a helpful assistant.' },
        { role: 'user', content: 'Explain quantum computing briefly.' }
    ]);

    console.log('Response:', response.choices[0].message.content);
    console.log('Tokens used:', response.usage.total_tokens);

    // 5. 清理
    await model.unload();
    console.log('Model unloaded');
}

main().catch(console.error);
```

## 参考文档

- [API 格式完整文档](API_FORMATS.md)
- [OpenAI API 参考](https://platform.openai.com/docs/api-reference)
- [Foundry Local 文档](https://aka.ms/foundry-local-docs)

## 总结

配置 Foundry Local 使用 OpenAPI 兼容格式非常简单：

1. **默认即可**: Foundry Local 默认使用 OpenAI 兼容格式，无需额外配置
2. **明确指定**: 如需明确指定，使用 `webServiceApiFormat: ApiFormat.OpenAI`
3. **完全兼容**: 可以直接使用 OpenAI SDK 连接到本地服务
4. **标准端点**: 提供完整的 `/v1/*` OpenAPI 端点
5. **灵活切换**: 可以随时在 OpenAI 和 Claude 格式间切换
