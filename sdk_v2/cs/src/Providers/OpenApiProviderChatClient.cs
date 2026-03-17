// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.AI.Foundry.Local.Providers;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Betalgo.Ranul.OpenAI.ObjectModels.ResponseModels;

using Microsoft.AI.Foundry.Local.OpenAI;
using Microsoft.Extensions.Logging;

/// <summary>
/// API provider client for OpenAPI-compatible services.
/// </summary>
internal class OpenApiProviderChatClient : IApiProviderChatClient
{
    private readonly string _host;
    private readonly string _apiToken;
    private readonly string _modelName;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public OpenApiProviderChatClient(string host, string apiToken, string modelName, ILogger logger)
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
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<ChatCompletionCreateResponse> CompleteChatAsync(
        IEnumerable<ChatMessage> messages,
        IEnumerable<ToolDefinition>? tools,
        OpenAIChatClient.ChatSettings settings,
        CancellationToken? ct = null)
    {
        var request = ChatCompletionCreateRequestExtended.FromUserInput(_modelName, messages, tools, settings);
        var requestJson = request.ToJson();

        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/v1/chat/completions", content, ct ?? CancellationToken.None);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct ?? CancellationToken.None);
        var chatCompletion = JsonSerializer.Deserialize<ChatCompletionCreateResponse>(
            responseJson,
            Detail.JsonSerializationContext.Default.ChatCompletionCreateResponse);

        if (chatCompletion == null)
        {
            throw new FoundryLocalException("Failed to deserialize chat completion response from API provider.", _logger);
        }

        return chatCompletion;
    }

    public async IAsyncEnumerable<ChatCompletionCreateResponse> CompleteChatStreamingAsync(
        IEnumerable<ChatMessage> messages,
        IEnumerable<ToolDefinition>? tools,
        OpenAIChatClient.ChatSettings settings,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var streamingSettings = settings with { Stream = true };
        var request = ChatCompletionCreateRequestExtended.FromUserInput(_modelName, messages, tools, streamingSettings);
        var requestJson = request.ToJson();

        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

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
                if (data == "[DONE]")
                {
                    break;
                }

                ChatCompletionCreateResponse? chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<ChatCompletionCreateResponse>(
                        data,
                        Detail.JsonSerializationContext.Default.ChatCompletionCreateResponse);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming chunk: {Data}", data);
                    continue;
                }

                if (chunk != null)
                {
                    yield return chunk;
                }
            }
        }
    }
}
