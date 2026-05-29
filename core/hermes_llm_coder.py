# hermes_llm_coder.py
# LLM-powered coding for Hermes agents

import openai

class LLMCoder:
    def __init__(self, api_key):
        openai.api_key = api_key

    def code(self, prompt, model='gpt-4'):
        response = openai.ChatCompletion.create(
            model=model,
            messages=[{"role": "user", "content": prompt}]
        )
        return response['choices'][0]['message']['content']

# Example: LLMCoder(api_key).code('Write a Python function to add two numbers')
