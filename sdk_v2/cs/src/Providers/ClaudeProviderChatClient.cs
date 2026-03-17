// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.AI.Foundry.Local.Providers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Betalgo.Ranul.OpenAI.ObjectModels;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Betalgo.Ranul.OpenAI.ObjectModels.ResponseModels;

using Microsoft.Extensions.Logging;

/// <summary>
/// API provider client for Claude-compatible services (Anthropic API).
/// Converts OpenAI-compatible requests to Claude format and responses back to OpenAI format.
/// </summary>
internal class ClaudeProviderChatClient : IApiProviderChatClient
{
    private readonly string _host;
    private readonly string _apiToken;
    private readonly string _modelName;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public ClaudeProviderChatClient(string host, string apiToken, string modelName, ILogger logger)
    {
        _host = host.TrimEnd('/');
        _apiToken = apiToken;
        _modelName = modelName;
        _logger = logger;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_host),
            Timeout = TimeSpan.FromMinutes(5)
        };
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiToken);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<ChatCompletionCreateResponse> CompleteChatAsync(
        IEnumerable<ChatMessage> messages,
        IEnumerable<ToolDefinition>? tools,
        OpenAIChatClient.ChatSettings settings,
        CancellationToken? ct = null)
    {
        var claudeRequest = ConvertToClaudeRequest(messages, tools, settings);
        var requestJson = JsonSerializer.Serialize(claudeRequest);

        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/v1/messages", content, ct ?? CancellationToken.None);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct ?? CancellationToken.None);
        var claudeResponse = JsonSerializer.Deserialize<JsonObject>(responseJson);

        if (claudeResponse == null)
        {
            throw new FoundryLocalException("Failed to deserialize Claude response from API provider.", _logger);
        }

        return ConvertToOpenAIResponse(claudeResponse);
    }

    public async IAsyncEnumerable<ChatCompletionCreateResponse> CompleteChatStreamingAsync(
        IEnumerable<ChatMessage> messages,
        IEnumerable<ToolDefinition>? tools,
        OpenAIChatClient.ChatSettings settings,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var claudeRequest = ConvertToClaudeRequest(messages, tools, settings);
        claudeRequest["stream"] = true;
        var requestJson = JsonSerializer.Serialize(claudeRequest);

        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        var responseId = Guid.NewGuid().ToString();
        var createdUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);

                JsonObject? claudeChunk;
                try
                {
                    claudeChunk = JsonSerializer.Deserialize<JsonObject>(data);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse Claude streaming chunk: {Data}", data);
                    continue;
                }

                if (claudeChunk == null)
                {
                    continue;
                }

                var eventType = claudeChunk["type"]?.GetValue<string>();

                if (eventType == "content_block_delta")
                {
                    var delta = claudeChunk["delta"]?.AsObject();
                    if (delta != null && delta["type"]?.GetValue<string>() == "text_delta")
                    {
                        var text = delta["text"]?.GetValue<string>() ?? string.Empty;
                        yield return new ChatCompletionCreateResponse
                        {
                            Id = responseId,
                            Object = "chat.completion.chunk",
                            Created = (int)createdUnix,
                            Model = _modelName,
                            Choices = new List<ChatChoiceResponse>
                            {
                                new ChatChoiceResponse
                                {
                                    Delta = new ChatMessage("assistant", text),
                                    Index = 0,
                                    FinishReason = null
                                }
                            }
                        };
                    }
                }
                else if (eventType == "message_stop")
                {
                    yield return new ChatCompletionCreateResponse
                    {
                        Id = responseId,
                        Object = "chat.completion.chunk",
                        Created = (int)createdUnix,
                        Model = _modelName,
                        Choices = new List<ChatChoiceResponse>
                        {
                            new ChatChoiceResponse
                            {
                                Delta = new ChatMessage("assistant", string.Empty),
                                Index = 0,
                                FinishReason = "stop"
                            }
                        }
                    };
                    break;
                }
            }
        }
    }

    private JsonObject ConvertToClaudeRequest(
        IEnumerable<ChatMessage> messages,
        IEnumerable<ToolDefinition>? tools,
        OpenAIChatClient.ChatSettings settings)
    {
        var messagesList = messages.ToList();
        var claudeRequest = new JsonObject
        {
            ["model"] = _modelName,
            ["max_tokens"] = settings.MaxTokens ?? 4096
        };

        // Extract system message if present
        var systemMessage = messagesList.FirstOrDefault(m => m.Role == StaticValues.ChatMessageRoles.System);
        if (systemMessage != null)
        {
            claudeRequest["system"] = systemMessage.Content;
            messagesList = messagesList.Where(m => m.Role != StaticValues.ChatMessageRoles.System).ToList();
        }

        // Convert messages to Claude format
        var claudeMessages = new JsonArray();
        foreach (var message in messagesList)
        {
            var claudeMessage = new JsonObject
            {
                ["role"] = message.Role == StaticValues.ChatMessageRoles.Assistant ? "assistant" : "user",
                ["content"] = message.Content ?? string.Empty
            };
            claudeMessages.Add(claudeMessage);
        }
        claudeRequest["messages"] = claudeMessages;

        // Add optional parameters
        if (settings.Temperature.HasValue)
        {
            claudeRequest["temperature"] = settings.Temperature.Value;
        }

        if (settings.TopP.HasValue)
        {
            claudeRequest["top_p"] = settings.TopP.Value;
        }

        return claudeRequest;
    }

    private ChatCompletionCreateResponse ConvertToOpenAIResponse(JsonObject claudeResponse)
    {
        var id = claudeResponse["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString();
        var content = new StringBuilder();

        var contentArray = claudeResponse["content"]?.AsArray();
        if (contentArray != null)
        {
            foreach (var contentBlock in contentArray)
            {
                var block = contentBlock?.AsObject();
                if (block != null && block["type"]?.GetValue<string>() == "text")
                {
                    content.Append(block["text"]?.GetValue<string>() ?? string.Empty);
                }
            }
        }

        var stopReason = claudeResponse["stop_reason"]?.GetValue<string>();
        var finishReason = stopReason switch
        {
            "end_turn" => "stop",
            "max_tokens" => "length",
            "stop_sequence" => "stop",
            _ => "stop"
        };

        return new ChatCompletionCreateResponse
        {
            Id = id,
            Object = "chat.completion",
            Created = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = _modelName,
            Choices = new List<ChatChoiceResponse>
            {
                new ChatChoiceResponse
                {
                    Message = new ChatMessage("assistant", content.ToString()),
                    Index = 0,
                    FinishReason = finishReason
                }
            },
            Usage = new UsageResponse
            {
                PromptTokens = claudeResponse["usage"]?["input_tokens"]?.GetValue<int>() ?? 0,
                CompletionTokens = claudeResponse["usage"]?["output_tokens"]?.GetValue<int>() ?? 0
            }
        };
    }
}
