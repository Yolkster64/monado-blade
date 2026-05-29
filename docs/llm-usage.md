# LLM Usage Guide

This guide explains how to use Large Language Models (LLMs) within Hermes Fleet Platforms.

## Integration
- Use provider adapters in `/llm-orchestration`.
- Supported providers: OpenAI, Azure OpenAI, and local model runtimes.
- Keep provider credentials in environment variables (`.env`), never in source files.

## Usage
- Use orchestration helpers to route prompts to the right provider/model.
- Add retry/backoff for transient API failures.
- Capture request/response metadata for observability and cost tracking.

## Best Practices
- Sanitize user input before prompt composition.
- Keep prompts versioned so behavior changes are traceable.
- Enforce policy checks before returning model output to end users.

## Example
```js
const response = await fetch('/api/llm/completion', { method: 'POST', body: JSON.stringify({ prompt: 'Hello, world!' }) });
const data = await response.json();
console.log(data.result);
```
