/**
 * Claude (Anthropic) API type definitions
 * These types represent the Claude Messages API format
 */

/**
 * Claude API message role
 */
export type ClaudeRole = 'user' | 'assistant';

/**
 * Claude API content block types
 */
export type ClaudeContentBlock =
    | { type: 'text'; text: string }
    | { type: 'tool_use'; id: string; name: string; input: any }
    | { type: 'tool_result'; tool_use_id: string; content: string };

/**
 * Claude API message
 */
export interface ClaudeMessage {
    role: ClaudeRole;
    content: string | ClaudeContentBlock[];
}

/**
 * Claude API tool definition
 */
export interface ClaudeTool {
    name: string;
    description: string;
    input_schema: {
        type: 'object';
        properties: Record<string, any>;
        required?: string[];
    };
}

/**
 * Claude API request parameters
 */
export interface ClaudeMessagesRequest {
    model: string;
    messages: ClaudeMessage[];
    max_tokens: number;
    system?: string;
    temperature?: number;
    top_p?: number;
    top_k?: number;
    tools?: ClaudeTool[];
    stream?: boolean;
    metadata?: {
        user_id?: string;
    };
    stop_sequences?: string[];
}

/**
 * Claude API usage information
 */
export interface ClaudeUsage {
    input_tokens: number;
    output_tokens: number;
}

/**
 * Claude API response
 */
export interface ClaudeMessagesResponse {
    id: string;
    type: 'message';
    role: 'assistant';
    content: ClaudeContentBlock[];
    model: string;
    stop_reason: 'end_turn' | 'max_tokens' | 'stop_sequence' | 'tool_use' | null;
    stop_sequence?: string | null;
    usage: ClaudeUsage;
}

/**
 * Claude API streaming event types
 */
export type ClaudeStreamEvent =
    | { type: 'message_start'; message: Partial<ClaudeMessagesResponse> }
    | { type: 'content_block_start'; index: number; content_block: Partial<ClaudeContentBlock> }
    | { type: 'content_block_delta'; index: number; delta: { type: 'text_delta'; text: string } | { type: 'input_json_delta'; partial_json: string } }
    | { type: 'content_block_stop'; index: number }
    | { type: 'message_delta'; delta: { stop_reason?: string; stop_sequence?: string }; usage: Partial<ClaudeUsage> }
    | { type: 'message_stop' }
    | { type: 'ping' }
    | { type: 'error'; error: { type: string; message: string } };
