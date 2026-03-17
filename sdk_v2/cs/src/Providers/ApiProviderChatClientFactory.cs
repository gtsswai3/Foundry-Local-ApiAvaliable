// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.AI.Foundry.Local.Providers;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Betalgo.Ranul.OpenAI.ObjectModels.ResponseModels;

using Microsoft.Extensions.Logging;

/// <summary>
/// Factory for creating API provider chat clients based on provider type.
/// </summary>
internal static class ApiProviderChatClientFactory
{
    /// <summary>
    /// Create an API provider chat client based on the model's configuration.
    /// </summary>
    /// <param name="modelInfo">The model information containing provider configuration.</param>
    /// <param name="logger">Logger instance.</param>
    /// <returns>An API provider chat client, or null if the model doesn't use an external API provider.</returns>
    public static IApiProviderChatClient? Create(ModelInfo modelInfo, ILogger logger)
    {
        // Check if this model uses an external API provider
        if (modelInfo.ApiProviderConfig == null)
        {
            return null;
        }

        var config = modelInfo.ApiProviderConfig;
        if (string.IsNullOrEmpty(config.Host) || string.IsNullOrEmpty(config.ApiToken))
        {
            throw new FoundryLocalException(
                $"API provider configuration for model {modelInfo.Id} is incomplete. Host and ApiToken are required.",
                logger);
        }

        var modelName = config.ModelName ?? modelInfo.Name;

        // Determine provider type from ProviderType or infer from config
        var providerType = modelInfo.ProviderType?.ToLowerInvariant();

        return providerType switch
        {
            "openai" or "openai-compatible" => new OpenApiProviderChatClient(config.Host, config.ApiToken, modelName, logger),
            "claude" or "claude-compatible" or "anthropic" => new ClaudeProviderChatClient(config.Host, config.ApiToken, modelName, logger),
            _ => throw new FoundryLocalException(
                $"Unsupported API provider type '{providerType}' for model {modelInfo.Id}. " +
                "Supported types: openai, openai-compatible, claude, claude-compatible, anthropic.",
                logger)
        };
    }
}

/// <summary>
/// Chat client that wraps either a local model or an external API provider.
/// </summary>
internal class UnifiedChatClient
{
    private readonly string _modelId;
    private readonly IApiProviderChatClient? _apiProviderClient;
    private readonly bool _isApiProvider;
    private readonly ILogger _logger;

    public UnifiedChatClient(string modelId, IApiProviderChatClient? apiProviderClient, ILogger logger)
    {
        _modelId = modelId;
        _apiProviderClient = apiProviderClient;
        _isApiProvider = apiProviderClient != null;
        _logger = logger;
    }

    public bool IsApiProvider => _isApiProvider;

    public async Task<ChatCompletionCreateResponse> CompleteChatAsync(
        IEnumerable<ChatMessage> messages,
        IEnumerable<ToolDefinition>? tools,
        OpenAIChatClient.ChatSettings settings,
        CancellationToken? ct = null)
    {
        if (_isApiProvider && _apiProviderClient != null)
        {
            return await _apiProviderClient.CompleteChatAsync(messages, tools, settings, ct);
        }

        // Fall through to use local model via CoreInterop
        // This will be handled by the OpenAIChatClient
        throw new FoundryLocalException(
            "This unified client is for API providers only. Use OpenAIChatClient for local models.",
            _logger);
    }

    public IAsyncEnumerable<ChatCompletionCreateResponse> CompleteChatStreamingAsync(
        IEnumerable<ChatMessage> messages,
        IEnumerable<ToolDefinition>? tools,
        OpenAIChatClient.ChatSettings settings,
        CancellationToken ct)
    {
        if (_isApiProvider && _apiProviderClient != null)
        {
            return _apiProviderClient.CompleteChatStreamingAsync(messages, tools, settings, ct);
        }

        // Fall through to use local model via CoreInterop
        throw new FoundryLocalException(
            "This unified client is for API providers only. Use OpenAIChatClient for local models.",
            _logger);
    }
}
