/**
 * API adapter for converting between OpenAI and Claude API formats
 */

import {
    ClaudeMessage,
    ClaudeMessagesRequest,
    ClaudeMessagesResponse,
    ClaudeTool,
    ClaudeContentBlock,
    ClaudeStreamEvent
} from './claude-types.js';

/**
 * OpenAI message structure
 */
interface OpenAIMessage {
    role: 'system' | 'user' | 'assistant' | 'tool';
    content: string;
    name?: string;
    tool_calls?: Array<{
        id: string;
        type: 'function';
        function: { name: string; arguments: string };
    }>;
    tool_call_id?: string;
}

/**
 * OpenAI chat completion request
 */
interface OpenAIChatRequest {
    model: string;
    messages: OpenAIMessage[];
    temperature?: number;
    top_p?: number;
    max_tokens?: number;
    stream?: boolean;
    tools?: Array<{
        type: 'function';
        function: {
            name: string;
            description: string;
            parameters: any;
        };
    }>;
    tool_choice?: any;
    frequency_penalty?: number;
    presence_penalty?: number;
    n?: number;
}

/**
 * OpenAI chat completion response
 */
interface OpenAIChatResponse {
    id: string;
    object: 'chat.completion';
    created: number;
    model: string;
    choices: Array<{
        index: number;
        message: {
            role: 'assistant';
            content: string | null;
            tool_calls?: Array<{
                id: string;
                type: 'function';
                function: { name: string; arguments: string };
            }>;
        };
        finish_reason: 'stop' | 'length' | 'tool_calls' | 'content_filter' | null;
    }>;
    usage: {
        prompt_tokens: number;
        completion_tokens: number;
        total_tokens: number;
    };
}

/**
 * Converts OpenAI request to Claude API format
 */
export function convertOpenAIToClaude(openaiRequest: OpenAIChatRequest): ClaudeMessagesRequest {
    // Extract system message if present
    let systemMessage: string | undefined;
    const messages: ClaudeMessage[] = [];

    for (const msg of openaiRequest.messages) {
        if (msg.role === 'system') {
            // Claude uses a top-level system parameter
            systemMessage = msg.content;
        } else if (msg.role === 'user' || msg.role === 'assistant') {
            // Convert tool calls to Claude format
            if (msg.role === 'assistant' && msg.tool_calls) {
                const contentBlocks: ClaudeContentBlock[] = [];

                // Add text content if present
                if (msg.content) {
                    contentBlocks.push({ type: 'text', text: msg.content });
                }

                // Add tool uses
                for (const toolCall of msg.tool_calls) {
                    contentBlocks.push({
                        type: 'tool_use',
                        id: toolCall.id,
                        name: toolCall.function.name,
                        input: JSON.parse(toolCall.function.arguments)
                    });
                }

                messages.push({
                    role: 'assistant',
                    content: contentBlocks
                });
            } else if (msg.role === 'user' && msg.tool_call_id) {
                // Convert tool result to Claude format
                messages.push({
                    role: 'user',
                    content: [{
                        type: 'tool_result',
                        tool_use_id: msg.tool_call_id,
                        content: msg.content
                    }]
                });
            } else {
                // Regular message
                messages.push({
                    role: msg.role as ClaudeRole,
                    content: msg.content
                });
            }
        }
    }

    // Convert tools to Claude format
    const tools: ClaudeTool[] | undefined = openaiRequest.tools?.map(tool => ({
        name: tool.function.name,
        description: tool.function.description,
        input_schema: tool.function.parameters
    }));

    const claudeRequest: ClaudeMessagesRequest = {
        model: openaiRequest.model,
        messages,
        max_tokens: openaiRequest.max_tokens || 1024,
        stream: openaiRequest.stream
    };

    if (systemMessage) {
        claudeRequest.system = systemMessage;
    }

    if (openaiRequest.temperature !== undefined) {
        claudeRequest.temperature = openaiRequest.temperature;
    }

    if (openaiRequest.top_p !== undefined) {
        claudeRequest.top_p = openaiRequest.top_p;
    }

    if (tools && tools.length > 0) {
        claudeRequest.tools = tools;
    }

    return claudeRequest;
}

/**
 * Converts Claude response to OpenAI format
 */
export function convertClaudeToOpenAI(claudeResponse: ClaudeMessagesResponse): OpenAIChatResponse {
    let contentText = '';
    const toolCalls: Array<{
        id: string;
        type: 'function';
        function: { name: string; arguments: string };
    }> = [];

    // Extract content and tool calls
    for (const block of claudeResponse.content) {
        if (block.type === 'text') {
            contentText += block.text;
        } else if (block.type === 'tool_use') {
            toolCalls.push({
                id: block.id,
                type: 'function',
                function: {
                    name: block.name,
                    arguments: JSON.stringify(block.input)
                }
            });
        }
    }

    // Determine finish reason
    let finishReason: 'stop' | 'length' | 'tool_calls' | 'content_filter' | null = null;
    if (claudeResponse.stop_reason === 'end_turn') {
        finishReason = 'stop';
    } else if (claudeResponse.stop_reason === 'max_tokens') {
        finishReason = 'length';
    } else if (claudeResponse.stop_reason === 'tool_use') {
        finishReason = 'tool_calls';
    }

    const openaiResponse: OpenAIChatResponse = {
        id: claudeResponse.id,
        object: 'chat.completion',
        created: Math.floor(Date.now() / 1000),
        model: claudeResponse.model,
        choices: [{
            index: 0,
            message: {
                role: 'assistant',
                content: contentText || null,
                ...(toolCalls.length > 0 && { tool_calls: toolCalls })
            },
            finish_reason: finishReason
        }],
        usage: {
            prompt_tokens: claudeResponse.usage.input_tokens,
            completion_tokens: claudeResponse.usage.output_tokens,
            total_tokens: claudeResponse.usage.input_tokens + claudeResponse.usage.output_tokens
        }
    };

    return openaiResponse;
}

/**
 * Converts Claude streaming event to OpenAI streaming format
 */
export function convertClaudeStreamToOpenAI(
    event: ClaudeStreamEvent,
    streamState: { id?: string; model?: string; contentBuffer: string[] }
): string | null {
    switch (event.type) {
        case 'message_start':
            if (event.message.id) streamState.id = event.message.id;
            if (event.message.model) streamState.model = event.message.model;
            return null;

        case 'content_block_start':
            // Initialize content buffer for this block index
            if (!streamState.contentBuffer[event.index]) {
                streamState.contentBuffer[event.index] = '';
            }
            return null;

        case 'content_block_delta':
            if (event.delta.type === 'text_delta') {
                const chunk = {
                    id: streamState.id || 'unknown',
                    object: 'chat.completion.chunk',
                    created: Math.floor(Date.now() / 1000),
                    model: streamState.model || 'unknown',
                    choices: [{
                        index: 0,
                        delta: {
                            content: event.delta.text
                        },
                        finish_reason: null
                    }]
                };
                return JSON.stringify(chunk);
            }
            return null;

        case 'message_delta':
            let finishReason: string | null = null;
            if (event.delta.stop_reason === 'end_turn') {
                finishReason = 'stop';
            } else if (event.delta.stop_reason === 'max_tokens') {
                finishReason = 'length';
            } else if (event.delta.stop_reason === 'tool_use') {
                finishReason = 'tool_calls';
            }

            if (finishReason) {
                const chunk = {
                    id: streamState.id || 'unknown',
                    object: 'chat.completion.chunk',
                    created: Math.floor(Date.now() / 1000),
                    model: streamState.model || 'unknown',
                    choices: [{
                        index: 0,
                        delta: {},
                        finish_reason: finishReason
                    }]
                };
                return JSON.stringify(chunk);
            }
            return null;

        case 'message_stop':
        case 'content_block_stop':
        case 'ping':
            return null;

        case 'error':
            return JSON.stringify({
                error: {
                    message: event.error.message,
                    type: event.error.type
                }
            });

        default:
            return null;
    }
}
