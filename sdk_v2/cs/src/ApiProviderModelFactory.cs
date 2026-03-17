// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.AI.Foundry.Local;

/// <summary>
/// Helper class for creating API provider model configurations.
/// </summary>
public static class ApiProviderModelFactory
{
    /// <summary>
    /// Create a ModelInfo for an OpenAI-compatible API provider.
    /// </summary>
    /// <param name="id">Unique identifier for the model.</param>
    /// <param name="name">Display name for the model.</param>
    /// <param name="host">API host URL (e.g., "https://api.openai.com").</param>
    /// <param name="apiToken">API token for authentication.</param>
    /// <param name="modelName">Optional model name to use with the provider. If not specified, uses the name parameter.</param>
    /// <param name="supportsToolCalling">Whether the model supports tool/function calling.</param>
    /// <param name="maxOutputTokens">Maximum output tokens the model supports.</param>
    /// <returns>A ModelInfo configured for an OpenAI-compatible API provider.</returns>
    public static ModelInfo CreateOpenAICompatibleModel(
        string id,
        string name,
        string host,
        string apiToken,
        string? modelName = null,
        bool supportsToolCalling = false,
        long maxOutputTokens = 4096)
    {
        return new ModelInfo
        {
            Id = id,
            Name = name,
            Version = 1,
            Alias = id,
            DisplayName = name,
            ProviderType = "openai-compatible",
            Uri = host,
            ModelType = "chat",
            Cached = false,
            Task = "chat-completion",
            SupportsToolCalling = supportsToolCalling,
            MaxOutputTokens = maxOutputTokens,
            CreatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ApiProviderConfig = new ApiProviderConfig
            {
                Host = host,
                ApiToken = apiToken,
                ModelName = modelName ?? name
            }
        };
    }

    /// <summary>
    /// Create a ModelInfo for a Claude-compatible API provider (Anthropic).
    /// </summary>
    /// <param name="id">Unique identifier for the model.</param>
    /// <param name="name">Display name for the model.</param>
    /// <param name="host">API host URL (e.g., "https://api.anthropic.com").</param>
    /// <param name="apiToken">API token for authentication.</param>
    /// <param name="modelName">Optional model name to use with the provider. If not specified, uses the name parameter.</param>
    /// <param name="maxOutputTokens">Maximum output tokens the model supports.</param>
    /// <returns>A ModelInfo configured for a Claude-compatible API provider.</returns>
    public static ModelInfo CreateClaudeCompatibleModel(
        string id,
        string name,
        string host,
        string apiToken,
        string? modelName = null,
        long maxOutputTokens = 4096)
    {
        return new ModelInfo
        {
            Id = id,
            Name = name,
            Version = 1,
            Alias = id,
            DisplayName = name,
            ProviderType = "claude-compatible",
            Uri = host,
            ModelType = "chat",
            Cached = false,
            Task = "chat-completion",
            SupportsToolCalling = false,
            MaxOutputTokens = maxOutputTokens,
            CreatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ApiProviderConfig = new ApiProviderConfig
            {
                Host = host,
                ApiToken = apiToken,
                ModelName = modelName ?? name
            }
        };
    }
}
