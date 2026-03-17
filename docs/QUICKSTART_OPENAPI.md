# Quick Start: OpenAPI Configuration

## TL;DR

Foundry Local uses **OpenAI-compatible API format by default**. No configuration needed!

```typescript
import { FoundryLocalManager } from 'foundry-local-sdk';

// That's it! Already using OpenAPI format
const manager = FoundryLocalManager.create({ appName: 'my-app' });
```

## Quick Examples

### JavaScript - Using Built-in Client

```javascript
import { FoundryLocalManager } from 'foundry-local-sdk';

const manager = FoundryLocalManager.create({ appName: 'my-app' });
const model = await manager.catalog.getModel('qwen2.5-0.5b');
await model.load();

const chatClient = model.createChatClient();
const response = await chatClient.completeChat([
    { role: 'user', content: 'Hello!' }
]);
console.log(response.choices[0].message.content);
```

### JavaScript - Using OpenAI SDK

```javascript
import OpenAI from 'openai';
import { FoundryLocalManager } from 'foundry-local-sdk';

const manager = FoundryLocalManager.create({
    appName: 'my-app',
    webServiceUrls: 'http://127.0.0.1:5000'
});
await manager.startWebService();

const client = new OpenAI({
    baseURL: `${manager.urls[0]}/v1`,
    apiKey: 'not-needed'
});

const response = await client.chat.completions.create({
    model: 'qwen2.5-0.5b',
    messages: [{ role: 'user', content: 'Hello!' }]
});
```

### C# - Using Built-in Client

```csharp
using Microsoft.AI.Foundry.Local;

var config = new Configuration { AppName = "my-app" };
await FoundryLocalManager.CreateAsync(config, logger);

var catalog = await FoundryLocalManager.Instance.GetCatalogAsync();
var model = await catalog.GetModelAsync("qwen2.5-0.5b");
await model.LoadAsync();

var chatClient = await model.GetChatClientAsync();
var response = await chatClient.CompleteChatAsync(messages);
```

### Python - Using OpenAI SDK

```python
from foundry_local import FoundryLocalManager
import openai

manager = FoundryLocalManager("qwen2.5-0.5b")
client = openai.OpenAI(base_url=manager.endpoint, api_key=manager.api_key)

response = client.chat.completions.create(
    model=manager.get_model_info("qwen2.5-0.5b").id,
    messages=[{"role": "user", "content": "Hello"}]
)
print(response.choices[0].message.content)
```

## Endpoints

All endpoints are under `/v1/`:

- `POST /v1/chat/completions` - Chat completions
- `GET /v1/models` - List models
- `POST /v1/responses` - Responses API

## More Info

- Full guide: [OPENAPI_CONFIG.md](OPENAPI_CONFIG.md)
- 中文指南: [OPENAPI_CONFIG_CN.md](OPENAPI_CONFIG_CN.md)
- API formats: [API_FORMATS.md](API_FORMATS.md)
