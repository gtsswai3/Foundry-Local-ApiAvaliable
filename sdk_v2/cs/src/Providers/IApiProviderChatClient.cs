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

/// <summary>
/// Interface for API provider chat clients that support OpenAI-compatible interfaces.
/// </summary>
internal interface IApiProviderChatClient
{
    /// <summary>
    /// Execute a chat completion request.
    /// </summary>
    Task<ChatCompletionCreateResponse> CompleteChatAsync(
        IEnumerable<ChatMessage> messages,
        IEnumerable<ToolDefinition>? tools,
        OpenAIChatClient.ChatSettings settings,
        CancellationToken? ct = null);

    /// <summary>
    /// Execute a chat completion request with streamed output.
    /// </summary>
    IAsyncEnumerable<ChatCompletionCreateResponse> CompleteChatStreamingAsync(
        IEnumerable<ChatMessage> messages,
        IEnumerable<ToolDefinition>? tools,
        OpenAIChatClient.ChatSettings settings,
        CancellationToken ct);
}
