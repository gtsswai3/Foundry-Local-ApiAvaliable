/**
 * Example demonstrating how to use the Claude API format with Foundry Local SDK
 *
 * This example shows:
 * 1. How to configure the SDK to use Claude API format
 * 2. How to use the ClaudeClient for chat completions
 * 3. How to stream responses using Claude API format
 */

import { FoundryLocalManager, ApiFormat, ClaudeClient } from 'foundry-local-sdk';

async function main() {
    // Configure the SDK with Claude API format
    const manager = FoundryLocalManager.create({
        appName: 'claude-api-example',
        webServiceApiFormat: ApiFormat.Claude  // Use Claude API format
    });

    console.log('Starting Foundry Local with Claude API format...');

    // Download and load a model
    const model = await manager.catalog.getModel('qwen2.5-0.5b');

    if (!model.isCached) {
        console.log('Downloading model...');
        await model.download((progress) => {
            process.stdout.write(`\rDownload progress: ${progress.toFixed(2)}%`);
        });
        console.log('\nDownload complete!');
    }

    console.log('Loading model...');
    await model.load();

    // Create a Claude API client
    const claudeClient: ClaudeClient = model.createClaudeClient();

    // Configure client settings
    claudeClient.settings.maxTokens = 512;
    claudeClient.settings.temperature = 0.7;
    claudeClient.settings.system = 'You are a helpful assistant.';

    console.log('\n--- Example 1: Non-streaming Claude API request ---');

    // Make a non-streaming request using Claude API format
    const response = await claudeClient.createMessage([
        {
            role: 'user',
            content: 'What is the golden ratio?'
        }
    ]);

    console.log('\nResponse:');
    for (const block of response.content) {
        if (block.type === 'text') {
            console.log(block.text);
        }
    }
    console.log(`\nStop reason: ${response.stop_reason}`);
    console.log(`Tokens used - Input: ${response.usage.input_tokens}, Output: ${response.usage.output_tokens}`);

    console.log('\n--- Example 2: Streaming Claude API request ---');

    // Make a streaming request using Claude API format
    process.stdout.write('\nStreaming response: ');
    await claudeClient.createMessageStreaming(
        [
            {
                role: 'user',
                content: 'Write a haiku about programming.'
            }
        ],
        (event) => {
            if (event.type === 'content_block_delta' && event.delta.type === 'text_delta') {
                process.stdout.write(event.delta.text);
            } else if (event.type === 'message_delta') {
                console.log(`\n\nStop reason: ${event.delta.stop_reason}`);
            }
        }
    );

    console.log('\n\n--- Example 3: Using Claude API with content blocks ---');

    // Using content blocks format (Claude's native format)
    const multiBlockResponse = await claudeClient.createMessage([
        {
            role: 'user',
            content: [
                {
                    type: 'text',
                    text: 'Explain quantum computing in one sentence.'
                }
            ]
        }
    ]);

    console.log('\nResponse with content blocks:');
    for (const block of multiBlockResponse.content) {
        if (block.type === 'text') {
            console.log(block.text);
        }
    }

    console.log('\n--- Example 4: Comparing OpenAI and Claude API formats ---');

    // OpenAI format client
    const openAIClient = model.createChatClient();
    openAIClient.settings.maxTokens = 50;

    const openAIResponse = await openAIClient.completeChat([
        { role: 'user', content: 'What is AI?' }
    ]);

    console.log('\nOpenAI format response:');
    console.log(openAIResponse.choices[0].message.content);

    // Claude format client
    claudeClient.settings.maxTokens = 50;
    const claudeResponse = await claudeClient.createMessage([
        { role: 'user', content: 'What is AI?' }
    ]);

    console.log('\nClaude format response:');
    for (const block of claudeResponse.content) {
        if (block.type === 'text') {
            console.log(block.text);
        }
    }

    // Cleanup
    await model.unload();
    console.log('\nModel unloaded. Example complete!');
}

main().catch((error) => {
    console.error('Error:', error);
    process.exit(1);
});
