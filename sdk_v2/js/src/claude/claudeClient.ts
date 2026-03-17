/**
 * Claude API compatible client wrapper
 * This client wraps the existing ChatClient and provides Claude API format support
 */

import { CoreInterop } from '../detail/coreInterop.js';
import {
    ClaudeMessage,
    ClaudeMessagesRequest,
    ClaudeMessagesResponse,
    ClaudeStreamEvent
} from '../claude-types.js';
import { convertOpenAIToClaude, convertClaudeToOpenAI, convertClaudeStreamToOpenAI } from '../api-adapter.js';

/**
 * Settings for Claude API client
 */
export class ClaudeClientSettings {
    maxTokens?: number;
    temperature?: number;
    topP?: number;
    topK?: number;
    system?: string;
    stopSequences?: string[];

    /**
     * Serializes the settings into a Claude-compatible request object
     * @internal
     */
    _serialize() {
        const result: any = {};

        if (this.maxTokens !== undefined) result.max_tokens = this.maxTokens;
        if (this.temperature !== undefined) result.temperature = this.temperature;
        if (this.topP !== undefined) result.top_p = this.topP;
        if (this.topK !== undefined) result.top_k = this.topK;
        if (this.system !== undefined) result.system = this.system;
        if (this.stopSequences !== undefined) result.stop_sequences = this.stopSequences;

        return result;
    }
}

/**
 * Client for performing chat completions with Claude API format
 * This client provides a Claude-compatible interface while using the underlying OpenAI infrastructure
 */
export class ClaudeClient {
    private modelId: string;
    private coreInterop: CoreInterop;

    /**
     * Configuration settings for Claude API requests
     */
    public settings = new ClaudeClientSettings();

    /**
     * @internal
     * Restricted to internal use because CoreInterop is an internal implementation detail.
     * Users should create clients via the Model.createClaudeClient() factory method.
     */
    constructor(modelId: string, coreInterop: CoreInterop) {
        this.modelId = modelId;
        this.coreInterop = coreInterop;
    }

    /**
     * Validates that messages array is properly formed for Claude API
     * @internal
     */
    private validateMessages(messages: ClaudeMessage[]): void {
        if (!messages || !Array.isArray(messages) || messages.length === 0) {
            throw new Error('Messages array cannot be null, undefined, or empty.');
        }

        for (const msg of messages) {
            if (!msg || typeof msg !== 'object' || Array.isArray(msg)) {
                throw new Error('Each message must be a non-null object with both "role" and "content" properties.');
            }
            if (typeof msg.role !== 'string' || msg.role.trim() === '') {
                throw new Error('Each message must have a "role" property that is a non-empty string.');
            }
            if (msg.role !== 'user' && msg.role !== 'assistant') {
                throw new Error('Each message role must be either "user" or "assistant".');
            }
            if (typeof msg.content !== 'string' && !Array.isArray(msg.content)) {
                throw new Error('Each message must have a "content" property that is either a string or an array of content blocks.');
            }
        }
    }

    /**
     * Performs a synchronous message completion using Claude API format
     * @param messages - An array of Claude API message objects
     * @param options - Additional request options that override client settings
     * @returns The Claude API message response object
     * @throws Error - If messages are invalid or completion fails
     */
    public async createMessage(
        messages: ClaudeMessage[],
        options?: Partial<ClaudeMessagesRequest>
    ): Promise<ClaudeMessagesResponse> {
        this.validateMessages(messages);

        // Build Claude request
        const claudeRequest: ClaudeMessagesRequest = {
            model: this.modelId,
            messages,
            max_tokens: options?.max_tokens || this.settings.maxTokens || 1024,
            ...this.settings._serialize(),
            ...options,
            stream: false
        };

        // Convert to OpenAI format for internal processing
        const openaiRequest = this._claudeToOpenAIRequest(claudeRequest);

        try {
            const response = this.coreInterop.executeCommand('chat_completions', {
                Params: { OpenAICreateRequest: JSON.stringify(openaiRequest) }
            });

            const openaiResponse = JSON.parse(response);

            // Convert OpenAI response back to Claude format
            return this._openAIToClaudeResponse(openaiResponse, claudeRequest);
        } catch (error) {
            throw new Error(
                `Claude message completion failed for model '${this.modelId}': ${error instanceof Error ? error.message : String(error)}`,
                { cause: error }
            );
        }
    }

    /**
     * Performs a streaming message completion using Claude API format
     * @param messages - An array of Claude API message objects
     * @param callback - A callback function that receives each streaming event
     * @param options - Additional request options that override client settings
     * @returns A promise that resolves when the stream is complete
     * @throws Error - If messages or callback are invalid, or streaming fails
     */
    public async createMessageStreaming(
        messages: ClaudeMessage[],
        callback: (event: ClaudeStreamEvent) => void,
        options?: Partial<ClaudeMessagesRequest>
    ): Promise<void> {
        this.validateMessages(messages);

        if (!callback || typeof callback !== 'function') {
            throw new Error('Callback must be a valid function.');
        }

        // Build Claude request
        const claudeRequest: ClaudeMessagesRequest = {
            model: this.modelId,
            messages,
            max_tokens: options?.max_tokens || this.settings.maxTokens || 1024,
            ...this.settings._serialize(),
            ...options,
            stream: true
        };

        // Convert to OpenAI format for internal processing
        const openaiRequest = this._claudeToOpenAIRequest(claudeRequest);

        let error: Error | null = null;
        const streamState = { id: '', model: this.modelId, contentBuffer: [] as string[] };

        try {
            await this.coreInterop.executeCommandStreaming(
                'chat_completions',
                { Params: { OpenAICreateRequest: JSON.stringify(openaiRequest) } },
                (chunkStr: string) => {
                    if (error) return;

                    if (chunkStr) {
                        try {
                            const openaiChunk = JSON.parse(chunkStr);
                            // Convert OpenAI streaming chunk to Claude event
                            const claudeEvent = this._openAIChunkToClaudeEvent(openaiChunk, streamState);
                            if (claudeEvent) {
                                callback(claudeEvent);
                            }
                        } catch (e) {
                            error = new Error(
                                `Failed to parse streaming chunk: ${e instanceof Error ? e.message : String(e)}`,
                                { cause: e }
                            );
                        }
                    }
                }
            );

            if (error) throw error;
        } catch (err) {
            const underlyingError = err instanceof Error ? err : new Error(String(err));
            throw new Error(`Streaming message completion failed for model '${this.modelId}': ${underlyingError.message}`, {
                cause: underlyingError
            });
        }
    }

    /**
     * Converts Claude request to OpenAI request format
     * @internal
     */
    private _claudeToOpenAIRequest(claudeRequest: ClaudeMessagesRequest): any {
        const openaiMessages: any[] = [];

        // Add system message if present
        if (claudeRequest.system) {
            openaiMessages.push({
                role: 'system',
                content: claudeRequest.system
            });
        }

        // Convert messages
        for (const msg of claudeRequest.messages) {
            if (typeof msg.content === 'string') {
                openaiMessages.push({
                    role: msg.role,
                    content: msg.content
                });
            } else {
                // Handle content blocks
                let textContent = '';
                const toolCalls: any[] = [];

                for (const block of msg.content) {
                    if (block.type === 'text') {
                        textContent += block.text;
                    } else if (block.type === 'tool_use') {
                        toolCalls.push({
                            id: block.id,
                            type: 'function',
                            function: {
                                name: block.name,
                                arguments: JSON.stringify(block.input)
                            }
                        });
                    } else if (block.type === 'tool_result') {
                        openaiMessages.push({
                            role: 'tool',
                            content: block.content,
                            tool_call_id: block.tool_use_id
                        });
                    }
                }

                if (textContent || toolCalls.length > 0) {
                    const message: any = {
                        role: msg.role,
                        content: textContent
                    };
                    if (toolCalls.length > 0) {
                        message.tool_calls = toolCalls;
                    }
                    openaiMessages.push(message);
                }
            }
        }

        const request: any = {
            model: claudeRequest.model,
            messages: openaiMessages,
            max_tokens: claudeRequest.max_tokens,
            stream: claudeRequest.stream
        };

        if (claudeRequest.temperature !== undefined) request.temperature = claudeRequest.temperature;
        if (claudeRequest.top_p !== undefined) request.top_p = claudeRequest.top_p;
        if (claudeRequest.top_k !== undefined) {
            request.metadata = { top_k: claudeRequest.top_k.toString() };
        }
        if (claudeRequest.tools) {
            request.tools = claudeRequest.tools.map(tool => ({
                type: 'function',
                function: {
                    name: tool.name,
                    description: tool.description,
                    parameters: tool.input_schema
                }
            }));
        }

        return request;
    }

    /**
     * Converts OpenAI response to Claude response format
     * @internal
     */
    private _openAIToClaudeResponse(openaiResponse: any, originalRequest: ClaudeMessagesRequest): ClaudeMessagesResponse {
        const choice = openaiResponse.choices[0];
        const content: any[] = [];

        if (choice.message.content) {
            content.push({
                type: 'text',
                text: choice.message.content
            });
        }

        if (choice.message.tool_calls) {
            for (const toolCall of choice.message.tool_calls) {
                content.push({
                    type: 'tool_use',
                    id: toolCall.id,
                    name: toolCall.function.name,
                    input: JSON.parse(toolCall.function.arguments)
                });
            }
        }

        let stopReason: 'end_turn' | 'max_tokens' | 'stop_sequence' | 'tool_use' | null = null;
        if (choice.finish_reason === 'stop') stopReason = 'end_turn';
        else if (choice.finish_reason === 'length') stopReason = 'max_tokens';
        else if (choice.finish_reason === 'tool_calls') stopReason = 'tool_use';

        return {
            id: openaiResponse.id,
            type: 'message',
            role: 'assistant',
            content,
            model: openaiResponse.model,
            stop_reason: stopReason,
            stop_sequence: null,
            usage: {
                input_tokens: openaiResponse.usage.prompt_tokens,
                output_tokens: openaiResponse.usage.completion_tokens
            }
        };
    }

    /**
     * Converts OpenAI streaming chunk to Claude event
     * @internal
     */
    private _openAIChunkToClaudeEvent(openaiChunk: any, streamState: any): ClaudeStreamEvent | null {
        const choice = openaiChunk.choices?.[0];
        if (!choice) return null;

        if (choice.delta?.content) {
            return {
                type: 'content_block_delta',
                index: 0,
                delta: {
                    type: 'text_delta',
                    text: choice.delta.content
                }
            };
        }

        if (choice.finish_reason) {
            let stopReason: string | undefined;
            if (choice.finish_reason === 'stop') stopReason = 'end_turn';
            else if (choice.finish_reason === 'length') stopReason = 'max_tokens';
            else if (choice.finish_reason === 'tool_calls') stopReason = 'tool_use';

            return {
                type: 'message_delta',
                delta: { stop_reason: stopReason },
                usage: {}
            };
        }

        return null;
    }
}
